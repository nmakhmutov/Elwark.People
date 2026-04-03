using Mediator;
using People.Application.Webhooks;
using People.Infrastructure;
using People.Infrastructure.Webhooks;

namespace People.Worker.Commands;

public sealed record CreateWebhookMessageCommand(long AccountId, WebhookType Type, DateTime OccurredAt) : ICommand;

public sealed class CreateWebhookMessageCommandHandler : ICommandHandler<CreateWebhookMessageCommand>
{
    private readonly WebhookDbContext _dbContext;

    public CreateWebhookMessageCommandHandler(WebhookDbContext dbContext) =>
        _dbContext = dbContext;

    public async ValueTask<Unit> Handle(CreateWebhookMessageCommand request, CancellationToken ct)
    {
        var message = new WebhookMessage(request.AccountId, request.Type, request.OccurredAt);
        await _dbContext.Messages.AddAsync(message, ct);
        await _dbContext.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
