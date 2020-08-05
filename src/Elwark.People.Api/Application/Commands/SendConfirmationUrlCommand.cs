using System;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.IntegrationEvents;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure.Services.Confirmation;
using Elwark.People.Api.Settings;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Infrastructure.Cache;
using Elwark.People.Infrastructure.Confirmation;
using Elwark.People.Shared.IntegrationEvents;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.Extensions.Options;

namespace Elwark.People.Api.Application.Commands
{
    public class SendConfirmationUrlCommand : IRequest
    {
        public SendConfirmationUrlCommand(AccountId accountId, IdentityId identityId, Notification notification,
            ConfirmationType confirmationType, UrlTemplate urlTemplate, CultureInfo cultureInfo)
        {
            IdentityId = identityId;
            Notification = notification;
            ConfirmationType = confirmationType;
            UrlTemplate = urlTemplate;
            CultureInfo = cultureInfo;
            AccountId = accountId;
        }

        public AccountId AccountId { get; }
        public IdentityId IdentityId { get; }

        public Notification Notification { get; }

        public ConfirmationType ConfirmationType { get; }

        public UrlTemplate UrlTemplate { get; }

        public CultureInfo CultureInfo { get; }
    }

    public class SendConfirmationUrlCommandHandler : IRequestHandler<SendConfirmationUrlCommand>
    {
        private readonly ICacheStorage _cache;
        private readonly IConfirmationService _confirmationService;
        private readonly IOAuthIntegrationEventService _eventService;
        private readonly IMediator _mediator;
        private readonly ConfirmationSettings _settings;
        private readonly IConfirmationStore _store;

        public SendConfirmationUrlCommandHandler(IConfirmationService confirmationService, ICacheStorage cache,
            IOAuthIntegrationEventService eventService, IConfirmationStore store,
            IOptions<ConfirmationSettings> settings, IMediator mediator)
        {
            _confirmationService = confirmationService ?? throw new ArgumentNullException(nameof(confirmationService));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public async Task<Unit> Handle(SendConfirmationUrlCommand request, CancellationToken cancellationToken)
        {
            var key = $"Confirmation_Url_{request.IdentityId}_{request.Notification}_{request.ConfirmationType}";
            var cache = await _cache.ReadAsync<DateTimeOffset?>(key);
            if (cache.HasValue)
                throw new ElwarkConfirmationException(ConfirmationError.AlreadySent, cache.Value);

            var notifier = request.Notification switch
            {
                Notification.NoneNotification _ => await _mediator.Send(
                    new GetNotifierQuery(request.AccountId, NotificationType.PrimaryEmail),
                    cancellationToken
                ),
                var x => x
            } ?? throw new ElwarkNotificationException(NotificationError.NotFound);

            var code = new Random()
                .Next(_settings.CodeRange.Min, _settings.CodeRange.Max);
            var confirmation = new ConfirmationModel(request.IdentityId, request.ConfirmationType, code,
                DateTimeOffset.UtcNow.Add(_settings.Link.Lifetime));

            await _store.CreateAsync(confirmation, cancellationToken);

            var token = _confirmationService.WriteToken(
                confirmation.Id, request.IdentityId, request.ConfirmationType, code
            );

            var url = request.UrlTemplate.Build(WebUtility.UrlEncode(token));

            await _eventService.PublishThroughEventBusAsync(
                new ConfirmationByUrlCreatedIntegrationEvent(
                    notifier,
                    url,
                    request.CultureInfo.TwoLetterISOLanguageName,
                    request.ConfirmationType),
                cancellationToken);

            await _cache.CreateAsync(key, DateTimeOffset.UtcNow.Add(_settings.Link.Delay), _settings.Link.Delay);
            return Unit.Value;
        }
    }
}