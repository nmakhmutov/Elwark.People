using People.Kafka.Integration;
using People.Webhooks.IntegrationEvents.Events;
using People.Webhooks.Model;
using People.Webhooks.Services.Retriever;
using People.Webhooks.Services.Sender;

namespace People.Webhooks.IntegrationEvents.EventHandling;

internal sealed class AccountUpdatedIntegrationEventHandler : IIntegrationEventHandler<AccountUpdatedIntegrationEvent>
{
    private readonly IWebhooksRetriever _retriever;
    private readonly IWebhooksSender _sender;

    public AccountUpdatedIntegrationEventHandler(IWebhooksRetriever retriever, IWebhooksSender sender)
    {
        _retriever = retriever;
        _sender = sender;
    }

    public async Task HandleAsync(AccountUpdatedIntegrationEvent message, CancellationToken ct)
    {
        var subscriptions = new List<WebhookSubscription>();

        await foreach (var subscription in _retriever.GetSubscribersAsync(WebhookType.Updated, ct))
            subscriptions.Add(subscription);

        var data = new WebhookData(message.AccountId, message.CreatedAt);
        await _sender.SendAll(subscriptions, data);
    }
}
