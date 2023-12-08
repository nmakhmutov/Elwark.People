using People.Webhooks.Model;

namespace People.Webhooks.Services.Retriever;

internal interface IWebhooksRetriever
{
    IAsyncEnumerable<WebhookSubscription> GetSubscribersAsync(WebhookType type, CancellationToken ct = default);
}
