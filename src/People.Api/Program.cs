using System;
using System.IO;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using Fluid;
using Fluid.MvcViewEngine;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using People.Api.Application.Behaviors;
using People.Api.Application.IntegrationEventHandlers;
using People.Api.Grpc;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Password;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Infrastructure;
using Integration.Event;
using Common.Kafka;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "Elwark.People.Api";
var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("application", appName)
    .WriteTo.Console(
        "json".Equals(builder.Configuration["Serilog:Formatter"])
            ? new CompactJsonFormatter()
            : new MessageTemplateTextFormatter(
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message}{NewLine}{Exception}")
    )
    .Destructure.ByTransforming<EmailMessageCreatedIntegrationEvent>(x => new { x.Email, x.Subject, x.CreatedAt })
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Logging
    .ClearProviders()
    .AddSerilog(logger);

builder.Services.AddOptions()
    .AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services
    .AddInfrastructure(builder.Configuration["App:Key"], builder.Configuration.GetSection("MongoDb").Bind)
    .Configure<PasswordValidationOptions>(builder.Configuration.GetSection("PasswordValidation"))
    .AddScoped<IPasswordValidator, PasswordValidator>();

builder.Services.AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Urls:GoogleApi"])
);

builder.Services.AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Urls:MicrosoftApi"])
);

builder.Services.AddKafkaMessageBus()
    .ConfigureProducers(config => config.BootstrapServers = builder.Configuration["Kafka:Servers"])
    .ConfigureConsumers(config =>
    {
        config.BootstrapServers = builder.Configuration["Kafka:Servers"];

        config.GroupId = appName;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        config.EnableAutoCommit = false;
        config.EnablePartitionEof = true;
        config.AllowAutoCreateTopics = true;
    })
    .AddProducer<AccountCreatedIntegrationEvent>(config => config.Topic = IntegrationEvent.CreatedAccounts)
    .AddProducer<AccountUpdatedIntegrationEvent>(config => config.Topic = IntegrationEvent.UpdatedAccounts)
    .AddProducer<EmailMessageCreatedIntegrationEvent>(config => config.Topic = IntegrationEvent.EmailMessages)
    .AddConsumer<AccountInfoReceivedIntegrationEvent, AccountInfoEventHandler>(config =>
    {
        config.Topic = IntegrationEvent.CollectedInformation;
        config.Threads = 2;
    });

var assemblies = AppDomain.CurrentDomain.GetAssemblies();
builder.Services
    .AddEmailBuilder(options =>
    {
        var path = Path.Combine(builder.Environment.ContentRootPath, "Email", "Views");

        options.ViewsPath = path;
        options.IncludesFileProvider = builder.Environment.ContentRootFileProvider;
        options.ViewsFileProvider = builder.Environment.ContentRootFileProvider;
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
        options.ViewLocationFormats.Add(Path.Combine(path, "{0}" + FluidViewEngine.ViewExtension));
    })
    .AddMediatR(assemblies)
    .AddValidatorsFromAssemblies(assemblies, ServiceLifetime.Scoped, null, true)
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

builder.Services.AddGrpc(options => options.Interceptors.Add<GlobalExceptionInterceptor>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var peopleContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    var infrastructureContext = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();

    await peopleContext.OnModelCreatingAsync();
    await infrastructureContext.OnModelCreatingAsync();

    // await new InfrastructureContextSeed(
    //     scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>(),
    //     scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>()
    // ).SeedAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    })
    .UseCorrelationId();

app.MapGrpcService<PeopleService>();
app.MapGrpcService<IdentityService>();
app.MapGrpcService<ManagementService>();

app.Run();
