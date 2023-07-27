using Microsoft.EntityFrameworkCore;
using People.Kafka;
using People.Webhooks.Infrastructure;
using People.Webhooks.IntegrationEvents.EventHandling;
using People.Webhooks.IntegrationEvents.Events;
using People.Webhooks.Services.Retriever;
using People.Webhooks.Services.Sender;
using Polly;
using Polly.Extensions.Http;
using Serilog;

const string appName = "People.Webhooks";
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDbContext<WebhookDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("Postgresql")!, optionsBuilder =>
        {
            optionsBuilder.EnableRetryOnFailure(5);
            optionsBuilder.CommandTimeout(60);
        });
        options.EnableDetailedErrors(builder.Environment.IsDevelopment());
        options.EnableSensitiveDataLogging(builder.Environment.IsDevelopment());
    });

builder.Services
    .AddTransient<IWebhooksRetriever, WebhooksRetriever>();

builder.Services
    .AddHttpClient<IWebhooksSender, WebhooksSender>()
    .AddPolicyHandler(
        HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(10, x => TimeSpan.FromSeconds(Math.Pow(2, x)))
    );

builder.Services
    .AddKafka(builder.Configuration.GetConnectionString("Kafka")!)
    .AddConsumer<AccountCreatedIntegrationEvent, AccountCreatedIntegrationEventHandler>(consumer =>
        consumer.WithTopic(KafkaTopic.CreatedAccounts)
            .WithGroupId(appName)
            .WithWorkers(2)
            .CreateTopicIfNotExists(2)
    )
    .AddConsumer<AccountUpdatedIntegrationEvent, AccountUpdatedIntegrationEventHandler>(consumer =>
        consumer.WithTopic(KafkaTopic.UpdatedAccounts)
            .WithGroupId(appName)
            .WithWorkers(4)
            .CreateTopicIfNotExists(4)
    )
    .AddConsumer<AccountDeletedIntegrationEvent, AccountDeletedIntegrationEventHandler>(consumer =>
        consumer.WithTopic(KafkaTopic.DeletedAccounts)
            .WithGroupId(appName)
            .WithWorkers(2)
            .CreateTopicIfNotExists(2)
    );

builder.Host
    .UseSerilog((context, configuration) => configuration
        .Enrich.WithProperty("ApplicationName", appName)
        .ReadFrom.Configuration(context.Configuration)
    );

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<WebhookDbContext>()
        .Database
        .MigrateAsync()
        .ConfigureAwait(false);

app.Run();
