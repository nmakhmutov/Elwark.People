using People.Api.Application.IntegrationEvents.Events;
using People.Domain.Repositories;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountEngagedIntegrationEventHandler : IIntegrationEventHandler<AccountActivity>
{
    private readonly IAccountRepository _repository;
    private readonly IConfirmationService _confirmation;
    private readonly TimeProvider _timeProvider;

    public AccountEngagedIntegrationEventHandler(
        IAccountRepository repository,
        IConfirmationService confirmation,
        TimeProvider timeProvider
    )
    {
        _confirmation = confirmation;
        _timeProvider = timeProvider;
        _repository = repository;
    }

    public async Task HandleAsync(AccountActivity message, CancellationToken ct)
    {
        var account = await _repository.GetAsync(message.AccountId, ct);
        if (account == null)
            return;

        switch (message)
        {
            case AccountActivity.InspectedIntegrationEvent:
                account.SetAsUpdated(_timeProvider);
                break;
            case AccountActivity.LoggedInIntegrationEvent:
                account.LoggedIn(message.CreatedAt);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message));
        }

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        await _confirmation.DeleteAsync(message.AccountId, ct);
    }
}
