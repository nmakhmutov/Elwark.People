using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Shared.Primitives;
using MediatR;

namespace Elwark.People.Api.Application.Commands
{
    public class SendConfirmationsAfterSignUpCommand : IRequest
    {
        public SendConfirmationsAfterSignUpCommand(AccountId accountId, UrlTemplate confirmationUrl,
            CultureInfo cultureInfo, IReadOnlyCollection<RegistrationIdentityResponse> identities)
        {
            AccountId = accountId;
            ConfirmationUrl = confirmationUrl;
            CultureInfo = cultureInfo;
            Identities = identities;
        }

        public AccountId AccountId { get; }

        public IReadOnlyCollection<RegistrationIdentityResponse> Identities { get; }

        public UrlTemplate ConfirmationUrl { get; }

        public CultureInfo CultureInfo { get; }
    }

    public class SendConfirmationsAfterSignUpCommandHandler : IRequestHandler<SendConfirmationsAfterSignUpCommand>
    {
        private readonly IMediator _mediator;

        private Notification.PrimaryEmail? _email;

        public SendConfirmationsAfterSignUpCommandHandler(IMediator mediator) =>
            _mediator = mediator;

        public async Task<Unit> Handle(SendConfirmationsAfterSignUpCommand request, CancellationToken cancellationToken)
        {
            foreach (var identity in request.Identities)
            {
                if (identity.IsConfirmed)
                    continue;

                Notification notification = identity.Notification switch
                {
                    Notification.PrimaryEmail email => email,
                    _ => await GetPrimaryEmail(request.AccountId, cancellationToken)
                };

                await _mediator.Send(
                    new SendConfirmationUrlCommand(
                        request.AccountId,
                        identity.IdentityId,
                        notification,
                        ConfirmationType.ConfirmIdentity,
                        request.ConfirmationUrl,
                        request.CultureInfo
                    ),
                    cancellationToken
                );
            }

            return Unit.Value;
        }

        private async ValueTask<Notification.PrimaryEmail> GetPrimaryEmail(AccountId id, CancellationToken ct)
        {
            if (_email is {})
                return _email;

            var result = await _mediator.Send(new GetNotifierQuery(id, NotificationType.PrimaryEmail), ct);
            if (result is Notification.PrimaryEmail email)
                _email = email;

            return _email!;
        }
    }
}