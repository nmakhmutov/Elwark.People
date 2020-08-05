using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Background.Services;
using Elwark.People.Shared.IntegrationEvents;

namespace Elwark.People.Background.EventHandlers
{
    public class AccountBanRemovedIntegrationEventHandler : IIntegrationEventHandler<AccountBanRemovedIntegrationEvent>
    {
        private readonly IEmailSendService _emailSendService;
        private readonly ITemplateBuilderService _templateBuilderService;

        public AccountBanRemovedIntegrationEventHandler(ITemplateBuilderService templateBuilderService,
            IEmailSendService emailSendService)
        {
            _templateBuilderService = templateBuilderService;
            _emailSendService = emailSendService;
        }

        public async Task HandleAsync(AccountBanRemovedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var template = $"AccountUnbanned.{evt.Language}.liquid";

            var email = await _templateBuilderService.CreateEmailAsync(template, null);

            await _emailSendService.SendAsync(evt.Email, email.Subject, email.Body, cancellationToken);
        }
    }
}