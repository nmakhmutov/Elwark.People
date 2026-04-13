using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using People.Infrastructure.Webhooks;
using Quartz;

namespace People.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed partial class WebhookDispatchJob : IJob
{
    private readonly IDbContextFactory<WebhookDbContext> _dbFactory;
    private readonly ILogger<WebhookDispatchJob> _logger;
    private readonly IWebhookSender _sender;

    public WebhookDispatchJob(
        IDbContextFactory<WebhookDbContext> dbFactory,
        IWebhookSender sender,
        ILogger<WebhookDispatchJob> logger
    )
    {
        _dbFactory = dbFactory;
        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var utcNow = context.FireTimeUtc.UtcDateTime;

        await using var db = await _dbFactory.CreateDbContextAsync(context.CancellationToken);

        var messages = await db.Messages
            .Where(x => x.Status == WebhookStatus.Pending && (x.RetryAfter == null || x.RetryAfter <= utcNow))
            .ToListAsync(context.CancellationToken);

        if (messages.Count == 0)
            return;

        var types = messages.Select(x => x.Type).Distinct();
        var consumers = await db.Consumers
            .AsNoTracking()
            .Where(x => types.Contains(x.Type))
            .GroupBy(x => x.Type)
            .ToDictionaryAsync(x => x.Key, context.CancellationToken);

        foreach (var message in messages)
        {
            try
            {
                if (consumers.TryGetValue(message.Type, out var list))
                {
                    var payload = new WebhookPayload(message.AccountId, message.OccurredAt);
                    await _sender.SendAsync(list, payload, context.CancellationToken);
                }

                db.Messages.Remove(message);
                MessageSent(message.Id);
            }
            catch (Exception exception)
            {
                message.MarkFailed(utcNow.AddMinutes(1));
                MessageFailed(message.Id, message.Attempts, message.Status, exception);
            }
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "Webhook message delivered and removed. MessageId={id}")]
    private partial void MessageSent(Guid id);

    [LoggerMessage(LogLevel.Warning,
        "Webhook message delivery failed. MessageId={id}, Attempts={attempts}, Status={status}")]
    private partial void MessageFailed(Guid id, int attempts, WebhookStatus status, Exception exception);
}
