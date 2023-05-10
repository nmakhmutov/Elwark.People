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

    public async Task HandleAsync(AccountUpdatedIntegrationEvent message)
    {
        var subscriptions = await _retriever.GetSubscribersAsync(WebhookType.Updated)
            .ConfigureAwait(false);

        await _sender.SendAll(subscriptions, new WebhookData(message.AccountId, WebhookType.Updated, message.CreatedAt))
            .ConfigureAwait(false);
    }
}
