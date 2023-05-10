using People.Kafka.Integration;
using People.Webhooks.IntegrationEvents.Events;
using People.Webhooks.Model;
using People.Webhooks.Services.Retriever;
using People.Webhooks.Services.Sender;

namespace People.Webhooks.IntegrationEvents.EventHandling;

internal sealed class AccountCreatedIntegrationEventHandler : IIntegrationEventHandler<AccountCreatedIntegrationEvent>
{
    private readonly IWebhooksRetriever _retriever;
    private readonly IWebhooksSender _sender;

    public AccountCreatedIntegrationEventHandler(IWebhooksRetriever retriever, IWebhooksSender sender)
    {
        _retriever = retriever;
        _sender = sender;
    }

    public async Task HandleAsync(AccountCreatedIntegrationEvent message)
    {
        var subscriptions = await _retriever.GetSubscribersAsync(WebhookType.Created)
            .ConfigureAwait(false);

        await _sender.SendAll(subscriptions, new WebhookData(message.AccountId, WebhookType.Created, message.CreatedAt))
            .ConfigureAwait(false);
    }
}
