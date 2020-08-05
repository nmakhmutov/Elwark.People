using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Background.Models;
using Elwark.People.Background.Services;
using Elwark.People.Shared.IntegrationEvents;

namespace Elwark.People.Background.EventHandlers
{
    public class AccountBanCreatedIntegrationEventHandler : IIntegrationEventHandler<AccountBanCreatedIntegrationEvent>
    {
        private readonly IEmailSendService _emailSendService;
        private readonly ITemplateBuilderService _templateBuilderService;

        public AccountBanCreatedIntegrationEventHandler(ITemplateBuilderService templateBuilderService,
            IEmailSendService emailSendService)
        {
            _templateBuilderService = templateBuilderService;
            _emailSendService = emailSendService;
        }

        public async Task HandleAsync(AccountBanCreatedIntegrationEvent evt, CancellationToken cancellationToken)
        {
            var template = $"AccountBanned.{evt.Language}.liquid";

            var model = new BanTemplateModel(evt.Reason, evt.Type.ToString());
            var email = await _templateBuilderService.CreateEmailAsync(template, model);

            await _emailSendService.SendAsync(evt.Email, email.Subject, email.Body, cancellationToken);
        }
    }
}