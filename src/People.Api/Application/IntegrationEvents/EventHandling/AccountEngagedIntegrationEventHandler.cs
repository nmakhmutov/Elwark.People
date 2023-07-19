using Microsoft.EntityFrameworkCore;
using People.Api.Application.IntegrationEvents.Events;
using People.Infrastructure;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.EventHandling;

internal sealed class AccountEngagedIntegrationEventHandler : IIntegrationEventHandler<AccountEngaged>
{
    private readonly PeopleDbContext _dbContext;
    private readonly ILogger<AccountEngagedIntegrationEventHandler> _logger;

    public AccountEngagedIntegrationEventHandler(PeopleDbContext dbContext,
        ILogger<AccountEngagedIntegrationEventHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleAsync(AccountEngaged message)
    {
        var property = message switch
        {
            AccountEngaged.CheckedActivityIntegrationEvent _ => "_lastActive",
            AccountEngaged.LoggedInIntegrationEvent _ => "_lastLogIn",
            _ => throw new ArgumentOutOfRangeException(nameof(message))
        };

        var result = await _dbContext.Accounts
            .Where(x => x.Id == message.AccountId)
            .ExecuteUpdateAsync(x => x.SetProperty(p => EF.Property<DateTime>(p, property), message.CreatedAt));

        if (result > 0)
            _logger.LogInformation("Account {id} {property} updated successful", message.AccountId, property);
        else
            _logger.LogWarning("Account {id} not found, engagement not updated", message.AccountId);
    }
}
