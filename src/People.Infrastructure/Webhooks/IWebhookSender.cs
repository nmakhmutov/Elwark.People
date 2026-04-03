using People.Application.Webhooks;

namespace People.Infrastructure.Webhooks;

public interface IWebhookSender
{
    Task SendAsync(long accountId, DateTime occurredAt, IEnumerable<WebhookConsumer> consumers, CancellationToken ct);
}
