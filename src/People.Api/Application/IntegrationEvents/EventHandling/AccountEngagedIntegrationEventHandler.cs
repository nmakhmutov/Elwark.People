using Microsoft.EntityFrameworkCore;
using People.Api.Application.IntegrationEvents.Events;
using People.Infrastructure;
using People.Infrastructure.Confirmations;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountEngagedIntegrationEventHandler : IIntegrationEventHandler<AccountActivity>
{
    private readonly IConfirmationService _confirmation;
    private readonly PeopleDbContext _dbContext;
    private readonly ILogger<AccountEngagedIntegrationEventHandler> _logger;

    public AccountEngagedIntegrationEventHandler(
        IConfirmationService confirmation,
        PeopleDbContext dbContext,
        ILogger<AccountEngagedIntegrationEventHandler> logger
    )
    {
        _confirmation = confirmation;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(AccountActivity message, CancellationToken ct)
    {
        var property = message switch
        {
            AccountActivity.InspectedIntegrationEvent _ => "_updatedAt",
            AccountActivity.LoggedInIntegrationEvent _ => "_lastLogIn",
            _ => throw new ArgumentOutOfRangeException(nameof(message))
        };

        var result = await _dbContext.Accounts
            .Where(x => x.Id == message.AccountId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => EF.Property<DateTime>(p, property), message.CreatedAt), ct);

        if (result > 0)
            _logger.LogInformation("Account {id} {property} updated successful", message.AccountId, property);
        else
            _logger.LogWarning("Account {id} not found, engagement not updated", message.AccountId);

        await _confirmation.DeleteAsync(message.AccountId, ct);
    }
}
