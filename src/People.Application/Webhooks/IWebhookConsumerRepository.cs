namespace People.Application.Webhooks;

public interface IWebhookConsumerRepository
{
    Task<WebhookConsumer?> GetAsync(Guid id, CancellationToken ct = default);

    IAsyncEnumerable<WebhookConsumer> GetAsync(CancellationToken ct = default);

    Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken ct = default);

    Task<WebhookConsumer> UpdateAsync(WebhookConsumer consumer, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
