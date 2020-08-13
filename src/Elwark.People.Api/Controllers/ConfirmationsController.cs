using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
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
    public class ConfirmationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ConfirmationsController(IMediator mediator) =>
            _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<ConfirmationModel>> GetAsync([FromQuery] string token, CancellationToken ct)
        {
            var confirmation = await _mediator.Send(new CheckConfirmationByTokenQuery(token), ct);
            var result = await _mediator.Send(new GetIdentityByIdQuery(confirmation.IdentityId), ct);

            return Ok(new ConfirmationModel(result.IdentityId, result.Identification));
        }

        [HttpPost]
        public async Task<ActionResult> PostAsync([FromBody] SendConfirmationRequest request, CancellationToken ct)
        {
            var identity = await _mediator.Send(new GetIdentityByIdentifierQuery(request.Email), ct)
                           ?? throw ElwarkIdentificationException.NotFound(request.Email);

            var email = await _mediator.Send(
                new GetNotifierQuery(identity.AccountId, NotificationType.PrimaryEmail), ct
            ) ?? throw new ElwarkNotificationException(NotificationError.NotFound);

            await _mediator.Send(
                new SendConfirmationUrlCommand(
                    identity.AccountId,
                    identity.IdentityId,
                    email,
                    ConfirmationType.ConfirmIdentity,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture),
                ct
            );

            return NoContent();
        }

        [HttpPut]
        public async Task<ActionResult<IdentityId>> PutAsync([FromBody] CheckConfirmationByTokenQuery query,
            CancellationToken ct)
        {
            var confirmation = await _mediator.Send(query, ct);
            await _mediator.Send(new ActivateIdentityCommand(confirmation.IdentityId), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.ConfirmationId), ct);

            return Ok(confirmation.IdentityId);
        }
    }
}