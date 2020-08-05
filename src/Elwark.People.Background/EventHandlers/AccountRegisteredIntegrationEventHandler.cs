using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Background.Services;
using Elwark.People.Shared.IntegrationEvents;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.EventHandlers
{
    public class AccountRegisteredIntegrationEventHandler : IIntegrationEventHandler<AccountRegisteredIntegrationEvent>
    {
        private static readonly IPAddress[] ForbiddenAddress =
        {
            IPAddress.Loopback,
            IPAddress.None,
            IPAddress.Any,
            IPAddress.IPv6Loopback,
            IPAddress.IPv6None,
            IPAddress.IPv6Any
        };

        private readonly IIntegrationEventPublisher _eventPublisher;
        private readonly IIpInformationService _ipService;
        private readonly ILogger<AccountRegisteredIntegrationEventHandler> _logger;

        public AccountRegisteredIntegrationEventHandler(IIpInformationService ipService,
            ILogger<AccountRegisteredIntegrationEventHandler> logger, IIntegrationEventPublisher eventPublisher)
        {
            _ipService = ipService ?? throw new ArgumentNullException(nameof(ipService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        }

        public async Task HandleAsync(AccountRegisteredIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var ip = IPAddress.Parse(evt.IpAddress);

            if (ForbiddenAddress.Contains(ip))
            {
                _logger.LogWarning("Ip address {ip} is forbidden", evt.IpAddress);
                return;
            }

            var ipInformation = await _ipService.GetIpInformationAsync(ip, evt.Language);
            if (ipInformation is {})
            {
                var integrationEvent = new MergeAccountInformationIntegrationEvent
                {
                    AccountId = evt.AccountId,
                    CountryCode = ipInformation.CountryCode,
                    City = ipInformation.City,
                    Timezone = ipInformation.Timezone
                };

                await _eventPublisher.PublishAsync(integrationEvent, cancellationToken);
            }
        }
    }
}