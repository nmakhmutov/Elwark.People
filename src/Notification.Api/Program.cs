using System.Net.Http.Headers;
using Common.Kafka;
using Common.Mongo;
using Confluent.Kafka;
using CorrelationId;
using CorrelationId.DependencyInjection;
using Integration.Event;
using Notification.Api.Grpc;
using Notification.Api.Infrastructure;
using Notification.Api.Infrastructure.Provider;
using Notification.Api.Infrastructure.Provider.Gmail;
using Notification.Api.Infrastructure.Provider.SendGrid;
using Notification.Api.Infrastructure.Repositories;
using Notification.Api.IntegrationEventHandlers;
using Notification.Api.Job;
using Quartz;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Display;

const string appName = "Notification.Api";
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
    .AddScoped<IEmailProviderRepository, EmailProviderRepository>()
    .AddScoped<IPostponedEmailRepository, PostponedEmailRepository>();

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

        configurator.ScheduleJob<PostponedEmailJob>(trigger => trigger
            .WithIdentity("PostponedEmailJob")
            .StartAt(DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow))
            .WithSimpleSchedule(scheduleBuilder => scheduleBuilder.WithIntervalInMinutes(1).RepeatForever())
        );
    })
    .AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

builder.Services.AddGrpc();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

    await context.OnModelCreatingAsync();
    await new NotificationContextSeed(context).SeedAsync();
}

app.UseCorrelationId();

app.MapGrpcService<NotificationService>();

app.Run();
