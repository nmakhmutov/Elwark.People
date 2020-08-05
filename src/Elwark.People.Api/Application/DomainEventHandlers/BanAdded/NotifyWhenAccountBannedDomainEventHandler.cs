using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Domain.Events;
using Elwark.People.Shared.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Api.Application.DomainEventHandlers.BanAdded
{
    public class NotifyWhenAccountBannedDomainEventHandler : INotificationHandler<BanAddedDomainEvent>
    {
        private readonly ILogger<INotificationHandler<BanAddedDomainEvent>> _logger;
        private readonly IOAuthIntegrationEventService _oauthIntegrationEventService;

        public NotifyWhenAccountBannedDomainEventHandler(ILogger<INotificationHandler<BanAddedDomainEvent>> logger,
            IOAuthIntegrationEventService oauthIntegrationEventService)
        {
            _logger = logger;
            _oauthIntegrationEventService = oauthIntegrationEventService;
        }

        public async Task Handle(BanAddedDomainEvent notification, CancellationToken cancellationToken)
        {
            var evt = new AccountBanCreatedIntegrationEvent(
                notification.Account.Id,
                notification.Account.GetPrimaryEmail(),
                notification.Ban.Type,
                notification.Ban.Reason,
                CultureInfo.CurrentCulture.TwoLetterISOLanguageName);

            await _oauthIntegrationEventService.PublishThroughEventBusAsync(evt, cancellationToken);

            _logger.LogInformation("Handler {event} for account {id}",
                nameof(NotifyWhenAccountBannedDomainEventHandler), evt.AccountId);
        }
    }
}