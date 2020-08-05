using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.EventBus;
using Elwark.People.Abstractions;
using Elwark.People.Background.Models;
using Elwark.People.Background.Services;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using Microsoft.Extensions.Logging;

namespace Elwark.People.Background.EventHandlers
{
    public class ConfirmationCreatedIntegrationEventHandler :
        IIntegrationEventHandler<ConfirmationByCodeCreatedIntegrationEvent>,
        IIntegrationEventHandler<ConfirmationByUrlCreatedIntegrationEvent>
    {
        private readonly IEmailSendService _emailSendService;
        private readonly ILogger<ConfirmationCreatedIntegrationEventHandler> _logger;
        private readonly ITemplateBuilderService _templateBuilderService;

        public ConfirmationCreatedIntegrationEventHandler(ILogger<ConfirmationCreatedIntegrationEventHandler> logger,
            ITemplateBuilderService templateBuilderService, IEmailSendService emailSendService)
        {
            _logger = logger;
            _templateBuilderService = templateBuilderService;
            _emailSendService = emailSendService;
        }

        public async Task HandleAsync(ConfirmationByCodeCreatedIntegrationEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("Confirmation by code event received for {0}", evt.Notification);

            var message = await CreateMessageAsync(evt.ConfirmationType, evt.Language, evt.Code);

            switch (evt.Notification)
            {
                case Notification.PrimaryEmail email:
                    await _emailSendService.SendAsync(email, message.Subject, message.Body, ct);
                    break;

                case Notification.SecondaryEmail email:
                    await _emailSendService.SendAsync(email, message.Subject, message.Body, ct);
                    break;
            }
        }

        public async Task HandleAsync(ConfirmationByUrlCreatedIntegrationEvent evt, CancellationToken ct)
        {
            _logger.LogInformation("Confirmation by url event received for {0}", evt.Notification);

            var message = await CreateMessageAsync(evt.ConfirmationType, evt.Language, evt.Url);

            switch (evt.Notification)
            {
                case Notification.PrimaryEmail email:
                    await _emailSendService.SendAsync(email, message.Subject, message.Body, ct);
                    break;

                case Notification.SecondaryEmail email:
                    await _emailSendService.SendAsync(email, message.Subject, message.Body, ct);
                    break;
            }
        }

        private async Task<EmailTemplateResult> CreateMessageAsync(ConfirmationType type, string language, Uri url)
        {
            var template = GetUrlConfirmationTemplateName(type, language);
            _logger.LogDebug("Email template name {0}", template);

            var model = new ConfirmationByUrlTemplateModel(url.ToString());

            return await _templateBuilderService.CreateEmailAsync(template, model);
        }

        private async Task<EmailTemplateResult> CreateMessageAsync(ConfirmationType type, string language, long code)
        {
            var template = GetCodeConfirmationTemplateName(language);
            _logger.LogDebug("Email template name {0} for type {1}", template, type);

            var model = new ConfirmationByCodeTemplateModel(code);

            return await _templateBuilderService.CreateEmailAsync(template, model);
        }

        private static string GetUrlConfirmationTemplateName(ConfirmationType type, string language) =>
            type switch
            {
                ConfirmationType.ConfirmIdentity => $"ConfirmEmailByUrl.{language}.liquid",
                ConfirmationType.UpdatePassword => $"ResetPassword.{language}.liquid",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        private static string GetCodeConfirmationTemplateName(string language) =>
            $"ConfirmationCode.{language}.liquid";
    }
}