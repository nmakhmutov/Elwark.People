using System;
using Common.Kafka;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using Fluid;
using Integration.Event;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using People.Api.Application.Behaviors;
using People.Api.Application.IntegrationEventHandlers;
using People.Api.Grpc;
using People.Api.Infrastructure;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Password;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Infrastructure;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "People.Api";
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
    .AddProducer<AccountDeletedIntegrationEvent>(config => config.Topic = IntegrationEvent.DeletedAccounts)
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
        options.ViewsFileProvider = builder.Environment.ContentRootFileProvider;
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
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
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    
    await peopleContext.OnModelCreatingAsync();
    await infrastructureContext.OnModelCreatingAsync();
    
    await new InfrastructureContextSeed(infrastructureContext, env)
        .SeedAsync();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    })
    .UseCorrelationId();

app.MapGrpcService<PeopleService>();
app.MapGrpcService<IdentityService>();
app.MapGrpcService<ManagementService>();
app.MapGrpcService<InfrastructureService>();

app.Run();
