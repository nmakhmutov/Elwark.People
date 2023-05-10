using People.Webhooks.Model;

namespace People.Webhooks.Services.Retriever;

internal interface IWebhooksRetriever
{
    Task<IReadOnlyCollection<WebhookSubscription>> GetSubscribersAsync(WebhookType type);
}
