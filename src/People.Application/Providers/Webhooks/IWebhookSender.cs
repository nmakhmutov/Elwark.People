namespace People.Application.Providers.Webhooks;

public interface IWebhookSender
{
    Task SendAsync(long accountId, DateTime occurredAt, IEnumerable<Webhook> subscriptions, CancellationToken ct);
}
