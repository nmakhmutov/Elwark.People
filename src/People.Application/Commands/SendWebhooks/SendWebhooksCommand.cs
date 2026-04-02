using Mediator;
using People.Application.Providers.Webhooks;

namespace People.Application.Commands.SendWebhooks;

public sealed record SendWebhooksCommand(long AccountId, WebhookType Type, DateTime OccurredAt) : ICommand;

public sealed class SendWebhooksCommandHandler : ICommandHandler<SendWebhooksCommand>
{
    private readonly IWebhookRetriever _retriever;
    private readonly IWebhookSender _webhookSender;

    public SendWebhooksCommandHandler(IWebhookRetriever retriever, IWebhookSender webhookSender)
    {
        _retriever = retriever;
        _webhookSender = webhookSender;
    }

    public async ValueTask<Unit> Handle(SendWebhooksCommand request, CancellationToken ct)
    {
        var subscriptions = await _retriever.RetrieveAsync(request.Type, ct)
            .ToListAsync(ct);

        if (subscriptions.Count == 0)
            return Unit.Value;

        await _webhookSender.SendAsync(request.AccountId, request.OccurredAt, subscriptions, ct);

        return Unit.Value;
    }
}
