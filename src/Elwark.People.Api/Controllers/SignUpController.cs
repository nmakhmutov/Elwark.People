using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Commands.SignUp;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController, Authorize(Policy = Policy.Identity)]
    public class SignUpController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SignUpController(IMediator mediator) =>
            _mediator = mediator;

        [HttpPost("email")]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByEmailRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByEmailCommand(request.Email, request.Password),
                ct
            );
            
            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("google")]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByGoogleRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByGoogleCommand(request.Google, request.Email, request.AccessToken),
                ct
            );

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("facebook")]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByFacebookRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByFacebookCommand(request.Facebook, request.Email, request.AccessToken),
                ct
            );

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        [HttpPost("microsoft")]
        public async Task<ActionResult<SignUpModel>> SignUpAsync([FromBody] SignUpByMicrosoftRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByMicrosoftCommand(request.Microsoft, request.Email, request.AccessToken),
                ct
            );

            await SendNotificationAsync(result, request.ConfirmationUrl, ct);

            return Ok(result);
        }

        private async Task SendNotificationAsync(SignUpModel model, UrlTemplate template, CancellationToken ct)
        {
            foreach (var identity in model.Identities)
            {
                if (identity.ConfirmedAt.HasValue)
                    continue;

                Notification notification = identity.Notification switch
                {
                    Notification.PrimaryEmail email => email,
                    _ => await GetPrimaryEmail(model.AccountId, ct)
                } ?? throw new ElwarkNotificationException(NotificationError.NotFound);

                await _mediator.Send(
                    new SendConfirmationUrlCommand(
                        model.AccountId,
                        identity.IdentityId,
                        notification,
                        ConfirmationType.ConfirmIdentity,
                        template,
                        CultureInfo.CurrentCulture
                    ),
                    ct
                );
            }
        }

        private Notification.PrimaryEmail? _primaryEmail;

        private async ValueTask<Notification.PrimaryEmail?> GetPrimaryEmail(AccountId id, CancellationToken ct)
        {
            if (_primaryEmail is {})
                return _primaryEmail;

            return _primaryEmail =
                await _mediator.Send(
                    new GetNotifierQuery(id, NotificationType.PrimaryEmail), ct
                ) as Notification.PrimaryEmail;
        }
    }
}