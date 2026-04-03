using People.Application.Webhooks;

namespace People.Infrastructure.Webhooks;

public sealed class WebhookMessage
{
    public Guid Id { get; private set; }

    public long AccountId { get; private set; }

    public WebhookType Type { get; private set; }

    public WebhookStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime? RetryAfter { get; private set; }

    public WebhookMessage(long accountId, WebhookType type, DateTime occurredAt)
        : this(Guid.CreateVersion7(), accountId, type, occurredAt, 0, null, WebhookStatus.Pending)
    {
    }

    private WebhookMessage(
        Guid id,
        long accountId,
        WebhookType type,
        DateTime occurredAt,
        int attempts,
        DateTime? retryAfter,
        WebhookStatus status
    )
    {
        Id = id;
        AccountId = accountId;
        Type = type;
        OccurredAt = occurredAt;
        Attempts = attempts;
        RetryAfter = retryAfter;
        Status = status;
    }

    public void MarkFailed(DateTime retryAfter)
    {
        Attempts++;
        if (Attempts >= 10)
        {
            Status = WebhookStatus.Failed;
            RetryAfter = null;
            return;
        }

        RetryAfter = retryAfter;
    }
}
