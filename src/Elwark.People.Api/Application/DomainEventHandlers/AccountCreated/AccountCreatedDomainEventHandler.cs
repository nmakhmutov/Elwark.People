using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Domain.Events;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.DomainEventHandlers.AccountCreated
{
    public class AccountCreatedDomainEventHandler : INotificationHandler<AccountCreatedDomainEvent>
    {
        private readonly ILogger<AccountCreatedDomainEventHandler> _logger;
        private readonly IOAuthIntegrationEventService _oauthIntegrationEventService;

        public AccountCreatedDomainEventHandler(ILogger<AccountCreatedDomainEventHandler> logger,
            IOAuthIntegrationEventService oauthIntegrationEventService)
        {
            _logger = logger;
            _oauthIntegrationEventService = oauthIntegrationEventService;
        }

        public async Task Handle(AccountCreatedDomainEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Handling account update domain event for {id}", notification.Account);

            var evt = new AccountCreatedIntegrationEvent(
                notification.Account.Id,
                notification.Account.GetPrimaryEmail()
            );
            await _oauthIntegrationEventService.PublishThroughEventBusAsync(evt, cancellationToken);
        }
    }
}