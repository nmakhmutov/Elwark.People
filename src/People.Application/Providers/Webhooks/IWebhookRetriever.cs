namespace People.Application.Providers.Webhooks;

public interface IWebhookRetriever
{
    IAsyncEnumerable<Webhook> RetrieveAsync(WebhookType type, CancellationToken ct);
}
