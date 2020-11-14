using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Settings;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace Elwark.People.Api.Application.Commands
{
    public class SendConfirmationCodeCommand : IRequest
    {
        public SendConfirmationCodeCommand(AccountId accountId, IdentityId identityId, Notification notification,
            ConfirmationType confirmationType, CultureInfo cultureInfo)
        {
            IdentityId = identityId;
            Notification = notification;
            ConfirmationType = confirmationType;
            CultureInfo = cultureInfo;
            AccountId = accountId;
        }

        public AccountId AccountId { get; }

        public IdentityId IdentityId { get; }

        public Notification Notification { get; }

        public ConfirmationType ConfirmationType { get; }

        public CultureInfo CultureInfo { get; }
    }

    public class SendConfirmationCodeCommandHandler : IRequestHandler<SendConfirmationCodeCommand>
    {
        private readonly IOAuthIntegrationEventService _eventService;
        private readonly IMediator _mediator;
        private readonly ConfirmationSettings _settings;
        private readonly IConfirmationStore _store;

        public SendConfirmationCodeCommandHandler(IOAuthIntegrationEventService eventService, IConfirmationStore store,
            IOptions<ConfirmationSettings> settings, IMediator mediator)
        {
            _eventService = eventService;
            _store = store;
            _settings = settings.Value;
            _mediator = mediator;
        }


        public async Task<Unit> Handle(SendConfirmationCodeCommand request, CancellationToken cancellationToken)
        {
            var cache = await _store.GetAsync(request.IdentityId, request.ConfirmationType);
            if (cache is {} && cache.CreatedAt.Add(_settings.Code.Delay) > DateTime.UtcNow)
                throw new ElwarkConfirmationAlreadySentException(cache.CreatedAt.Add(_settings.Code.Delay));

            var notifier = request.Notification switch
            {
                Notification.NoneNotification _ => await _mediator.Send(
                    new GetNotifierQuery(request.AccountId, NotificationType.PrimaryEmail),
                    cancellationToken
                ),
                var x => x
            } ?? throw new ElwarkNotificationException(NotificationError.NotFound);

            var code = new Random().Next(_settings.CodeRange.Min, _settings.CodeRange.Max);
            var confirmation = new ConfirmationModel(request.IdentityId, request.ConfirmationType, code);

            await _store.CreateAsync(confirmation, _settings.Link.Lifetime);

            await _eventService.PublishThroughEventBusAsync(
                new ConfirmationByCodeCreatedIntegrationEvent(
                    notifier,
                    code,
                    request.CultureInfo.TwoLetterISOLanguageName,
                    request.ConfirmationType),
                cancellationToken);

            return Unit.Value;
        }
    }
}