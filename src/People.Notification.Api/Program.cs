using System;
using System.Net.Http.Headers;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using People.Integration.Event;
using People.Kafka;
using People.Mongo;
using People.Notification.Api.Grpc;
using People.Notification.Api.Infrastructure;
using People.Notification.Api.Infrastructure.Provider;
using People.Notification.Api.Infrastructure.Provider.Gmail;
using People.Notification.Api.Infrastructure.Provider.SendGrid;
using People.Notification.Api.Infrastructure.Repositories;
using People.Notification.Api.IntegrationEventHandlers;
using People.Notification.Api.Job;
using Quartz;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "People.Notification.Api";
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

builder.Services
    .AddCorrelationId(options =>
    {
        options.UpdateTraceIdentifier = true;
        options.AddToLoggingScope = true;
    })
    .WithTraceIdentifierProvider();

builder.Services
    .Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb").Bind)
    .AddScoped<NotificationDbContext>()
    .AddScoped<IEmailProviderRepository, EmailProviderRepository>();

builder.Services
    .AddKafkaMessageBus()
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
    .AddProducer<EmailMessageCreatedIntegrationEvent>(config => config.Topic = IntegrationEvent.EmailMessages)
    .AddConsumer<EmailMessageCreatedIntegrationEvent, EmailMessageCreatedHandler>(config =>
    {
        config.Topic = IntegrationEvent.EmailMessages;
        config.Threads = 2;
    });

builder.Services
    .AddTransient<IEmailSender>(_ => new GmailProvider(
            builder.Configuration["Gmail:UserName"],
            builder.Configuration["Gmail:Password"]
        )
    );

builder.Services
    .AddHttpClient<IEmailSender, SendgridProvider>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Sendgrid:Host"]);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", builder.Configuration["Sendgrid:Key"]);
    });

builder.Services
    .AddQuartz(configurator =>
    {
        configurator.UseMicrosoftDependencyInjectionJobFactory();

        configurator.ScheduleJob<UpdateProviderJob>(trigger => trigger
            .WithIdentity("UpdateProviderJob")
            .StartAt(DateBuilder.EvenHourDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInHours(1).RepeatForever())
        );
    })
    .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await new NotificationContextSeed(scope.ServiceProvider.GetRequiredService<NotificationDbContext>()).SeedAsync();
}

app.UseCorrelationId();

app.MapGrpcService<NotificationService>();

app.Run();
