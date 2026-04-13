using People.Application.Webhooks;

namespace People.Infrastructure.Webhooks;

public interface IWebhookSender
{
    Task SendAsync(IEnumerable<WebhookConsumer> consumers, WebhookPayload payload, CancellationToken ct);
}
