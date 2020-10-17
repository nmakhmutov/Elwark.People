using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
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
    public class PasswordsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PasswordsController(IMediator mediator) =>
            _mediator = mediator;

        [HttpPost("reset")]
        public async Task<ActionResult> ResetAsync([FromBody] ResetPasswordRequest request, CancellationToken ct)
        {
            var identity = await _mediator.Send(new GetIdentityByIdentifierQuery(request.Email), ct)
                           ?? throw ElwarkIdentificationException.NotFound(request.Email);

            var email = await _mediator.Send(
                new GetNotifierQuery(identity.AccountId, NotificationType.PrimaryEmail), ct
            ) ?? throw new ElwarkNotificationException(NotificationError.NotFound);

            await _mediator.Send(new SendConfirmationUrlCommand(
                    identity.AccountId,
                    identity.IdentityId,
                    email,
                    ConfirmationType.UpdatePassword,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture),
                ct);

            return NoContent();
        }

        [HttpPut("restore")]
        public async Task<ActionResult> RestoreAsync([FromBody] RestorePasswordRequest request, CancellationToken ct)
        {
            var decode = await _mediator.Send(new DecodeConfirmationQuery(request.ConfirmationToken), ct);
            var confirmation = await _mediator.Send(
                new CheckConfirmationQuery(decode.IdentityId, decode.Type, decode.Code), ct
            );
            var identity = await _mediator.Send(new GetIdentityByIdQuery(confirmation.IdentityId), ct);

            await _mediator.Send(new UpdatePasswordCommand(identity.AccountId, null, request.Password), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.IdentityId, confirmation.Type), ct);

            return NoContent();
        }
    }
}