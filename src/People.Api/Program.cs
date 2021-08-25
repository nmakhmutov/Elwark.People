using System;
using System.Net;
using System.Net.Http.Headers;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using FluentValidation;
using Fluid;
using Fluid.MvcViewEngine;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using People.Api.Application.Behaviors;
using People.Api.Application.IntegrationEventHandlers;
using People.Api.Grpc;
using People.Api.Infrastructure;
using People.Api.Infrastructure.EmailBuilder;
using People.Api.Infrastructure.Interceptors;
using People.Api.Infrastructure.Password;
using People.Api.Infrastructure.Provider.Email;
using People.Api.Infrastructure.Provider.Email.Gmail;
using People.Api.Infrastructure.Provider.Email.SendGrid;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Host;
using People.Infrastructure;
using People.Infrastructure.IntegrationEvents;
using People.Infrastructure.Kafka;
using Serilog;

const string appName = "People.Api";
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
var configuration = HostExtensions.CreateConfiguration(environment, args);

Log.Logger = HostExtensions.CreateLogger(configuration, appName);

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddConfiguration(configuration);
builder.Services.AddOptions()
    .AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services
    .AddInfrastructure(configuration["App:Key"], configuration.GetSection("MongoDb").Bind)
    .Configure<PasswordValidationOptions>(configuration.GetSection("PasswordValidation"))
    .AddScoped<IPasswordValidator, PasswordValidator>();

builder.Services.AddHttpClient<IGoogleApiService, GoogleApiService>(client =>
    client.BaseAddress = new Uri(configuration["Urls:GoogleApi"])
);

builder.Services.AddHttpClient<IMicrosoftApiService, MicrosoftApiService>(client =>
    client.BaseAddress = new Uri(configuration["Urls:MicrosoftApi"])
);

builder.Services.AddKafkaMessageBus()
    .ConfigureProducers(config => config.BootstrapServers = configuration["Kafka:Servers"])
    .ConfigureConsumers(config =>
    {
        config.BootstrapServers = configuration["Kafka:Servers"];

        config.GroupId = appName;
        config.AutoOffsetReset = AutoOffsetReset.Earliest;
        config.EnableAutoCommit = false;
        config.EnablePartitionEof = true;
        config.AllowAutoCreateTopics = true;
    })
    .AddProducer<AccountCreatedIntegrationEvent>(config => config.Topic = IntegrationEvent.CreatedAccounts)
    .AddProducer<AccountUpdatedIntegrationEvent>(config => config.Topic = IntegrationEvent.UpdatedAccounts)
    .AddProducer<EmailMessageCreatedIntegrationEvent>(
        config => config.Topic = IntegrationEvent.EmailMessages
    )
    .AddConsumer<EmailMessageCreatedIntegrationEvent, EmailMessageCreatedHandler>(config =>
    {
        config.Topic = IntegrationEvent.EmailMessages;
        config.Threads = 2;
    })
    .AddConsumer<AccountInfoReceivedIntegrationEvent, AccountInfoEventHandler>(config =>
    {
        config.Topic = IntegrationEvent.CollectedInformation;
        config.Threads = 2;
    })
    .AddConsumer<ProviderExpiredIntegrationEvent, UpdateExpiredProviderEventHandler>(config =>
        config.Topic = IntegrationEvent.ExpiredProviders
    );

builder.Services.Configure<GmailOptions>(configuration.GetSection("Gmail"));
builder.Services.AddTransient<IEmailSender, GmailProvider>();
builder.Services.AddHttpClient<IEmailSender, SendgridProvider>(client =>
{
    client.BaseAddress = new Uri(configuration["Sendgrid:Host"]);
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", configuration["Sendgrid:Key"]);
});

var assemblies = AppDomain.CurrentDomain.GetAssemblies();
builder.Services
    .AddEmailBuilder(options =>
    {
        options.TemplateOptions.MemberAccessStrategy = UnsafeMemberAccessStrategy.Instance;
        options.ViewLocationFormats.Add("Email/Views/{0}" + FluidViewEngine.ViewExtension);
    })
    .AddMediatR(assemblies)
    .AddValidatorsFromAssemblies(assemblies)
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>))
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
    .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));

builder.Services.AddGrpc(options => options.Interceptors.Add<GlobalExceptionInterceptor>());

builder.Host.UseSerilog();
builder.WebHost.UseKestrel(options =>
    options.Listen(IPAddress.Any, int.Parse(configuration["Grpc:Port"]), x => x.Protocols = HttpProtocols.Http2));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var peopleContext = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
    var infrastructureContext = scope.ServiceProvider.GetRequiredService<InfrastructureDbContext>();
    
    await peopleContext.OnModelCreatingAsync();
    await infrastructureContext.OnModelCreatingAsync();

    await new PeopleContextSeed(peopleContext).SeedAsync();
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

app.MapGrpcService<GatewayService>();
app.MapGrpcService<IdentityService>();

app.Run();
