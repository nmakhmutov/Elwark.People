using Mediator;
using Microsoft.EntityFrameworkCore;
using People.Application.Commands.EnrichAccount;
using People.Application.Commands.SendWebhooks;
using People.Application.Providers.Webhooks;
using People.Domain.Events;
using People.Domain.IntegrationEvents;
using People.Infrastructure;
using People.Infrastructure.Outbox.Entities;
using People.Infrastructure.Outbox.Extensions;
using Quartz;

namespace People.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed partial class OutboxDispatchJob : IJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxDispatchJob> _logger;
    private readonly IDbContextFactory<PeopleDbContext> _dbFactory;

    public OutboxDispatchJob(
        IServiceScopeFactory scopeFactory,
        IDbContextFactory<PeopleDbContext> dbFactory,
        ILogger<OutboxDispatchJob> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _dbFactory = dbFactory;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var utcNow = context.FireTimeUtc.UtcDateTime;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        await using var dbContext = await _dbFactory.CreateDbContextAsync(context.CancellationToken);
        await using var tx = await dbContext.Database.BeginTransactionAsync(context.CancellationToken);

        var messages = await dbContext.GetPendingMessagesAsync(utcNow, 100, context.CancellationToken);
        if (messages.Count == 0)
            return;

        var ids = messages.Select(static x => x.Id).ToList();
        var processed = (await dbContext.OutboxConsumers
                .Where(x => ids.Contains(x.MessageId))
                .Select(static x => new { x.MessageId, x.Consumer })
                .ToListAsync(context.CancellationToken))
            .Select(static x => (x.MessageId, x.Consumer))
            .ToHashSet();

        foreach (var message in messages)
        {
            try
            {
                var payload = message.GetPayload();

                foreach (var command in GetCommands(payload))
                {
                    var consumer = command.GetType().Name;

                    if (processed.Contains((message.Id, consumer)))
                    {
                        CommandSkipped(message.Id, consumer);
                        continue;
                    }

                    await mediator.Send(command, context.CancellationToken);
                    dbContext.OutboxConsumers.Add(OutboxConsumer.Create(message.Id, consumer, utcNow));
                }

                message.MarkProcessed(utcNow);
                MessageProcessed(message.Id);
            }
            catch (Exception exception)
            {
                message.MarkFailed(utcNow, exception);
                MessageFailed(message.Status, message.Id, message.NextRetryAt, exception);
            }
        }

        await dbContext.SaveChangesAsync(context.CancellationToken);
        await tx.CommitAsync(context.CancellationToken);
    }

    private static IEnumerable<ICommand> GetCommands(IIntegrationEvent payload) =>
        payload switch
        {
            AccountCreatedIntegrationEvent x =>
            [
                new EnrichAccountCommand(x.AccountId, x.IpAddress),
                new SendWebhooksCommand(x.AccountId, WebhookType.Created, x.OccurredAt)
            ],
            AccountUpdatedIntegrationEvent x =>
            [
                new SendWebhooksCommand(x.AccountId, WebhookType.Updated, x.OccurredAt)
            ],
            AccountDeletedIntegrationEvent x =>
            [
                new SendWebhooksCommand(x.AccountId, WebhookType.Deleted, x.OccurredAt)
            ],
            _ => throw new ArgumentOutOfRangeException()
        };

    [LoggerMessage(LogLevel.Information, "Outbox message processed. MessageId={id}")]
    private partial void MessageProcessed(Guid id);

    [LoggerMessage(LogLevel.Information, "Outbox command skipped (idempotent). MessageId={id}, Consumer={consumer}")]
    private partial void CommandSkipped(Guid id, string consumer);

    [LoggerMessage(LogLevel.Error, "Outbox message failed {Status}. MessageId={id}, NextRetryAt={NextRetryAt}")]
    private partial void MessageFailed(OutboxStatus status, Guid id, DateTime? nextRetryAt, Exception exception);
}
