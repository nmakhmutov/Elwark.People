using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Domain.Events;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.DomainEventHandlers
{
    public class AccountUnbannedDomainEventHandler : INotificationHandler<BanRemovedDomainEvent>
    {
        private readonly ILogger<INotificationHandler<BanRemovedDomainEvent>> _logger;

        private readonly IOAuthIntegrationEventService _oauthIntegrationEventService;

        public AccountUnbannedDomainEventHandler(ILogger<INotificationHandler<BanRemovedDomainEvent>> logger,
            IOAuthIntegrationEventService oauthIntegrationEventService)
        {
            _logger = logger;
            _oauthIntegrationEventService = oauthIntegrationEventService;
        }

        public async Task Handle(BanRemovedDomainEvent notification, CancellationToken cancellationToken)
        {
            var evt = new AccountBanRemovedIntegrationEvent(notification.Account.Id,
                notification.Account.GetPrimaryEmail(),
                notification.Account.BasicInfo.Language.TwoLetterISOLanguageName);

            await _oauthIntegrationEventService.PublishThroughEventBusAsync(evt, cancellationToken);

            _logger.LogInformation("Handler {evt} for account {id}", nameof(BanRemovedDomainEvent), evt.AccountId);
        }
    }
}