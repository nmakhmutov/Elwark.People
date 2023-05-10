using People.Webhooks.Model;

namespace People.Webhooks.Services.Sender;

internal interface IWebhooksSender
{
    Task SendAll(IEnumerable<WebhookSubscription> receivers, WebhookData data);
}
