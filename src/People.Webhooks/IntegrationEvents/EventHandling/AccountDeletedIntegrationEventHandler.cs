using People.Kafka.Integration;
using People.Webhooks.IntegrationEvents.Events;
using People.Webhooks.Model;
using People.Webhooks.Services.Retriever;
using People.Webhooks.Services.Sender;

namespace People.Webhooks.IntegrationEvents.EventHandling;

internal sealed class AccountDeletedIntegrationEventHandler : IIntegrationEventHandler<AccountDeletedIntegrationEvent>
{
    private readonly IWebhooksRetriever _retriever;
    private readonly IWebhooksSender _sender;

    public AccountDeletedIntegrationEventHandler(IWebhooksRetriever retriever, IWebhooksSender sender)
    {
        _retriever = retriever;
        _sender = sender;
    }

    public async Task HandleAsync(AccountDeletedIntegrationEvent message, CancellationToken ct)
    {
        var subscriptions = new List<WebhookSubscription>();

        await foreach (var subscription in _retriever.GetSubscribersAsync(WebhookType.Deleted, ct))
            subscriptions.Add(subscription);

        var data = new WebhookData(message.AccountId, WebhookType.Deleted, message.CreatedAt);
        await _sender.SendAll(subscriptions, data);
    }
}
