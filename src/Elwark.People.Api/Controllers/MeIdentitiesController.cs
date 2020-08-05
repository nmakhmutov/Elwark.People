using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("accounts/me/identities"), ApiController, Authorize(Policy = Policy.Account)]
    public class MeIdentitiesController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IMediator _mediator;

        public MeIdentitiesController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IdentityResponse>>> GetIdentitiesAsync(CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var result = await _mediator.Send(new GetIdentitiesQuery(accountId), ct);

            return Ok(result);
        }

        [HttpPost("{id}/confirm")]
        public async Task<ActionResult> SendIdentityConfirmCodeAsync(IdentityId id, CancellationToken ct)
        {
            var identity = await _mediator.Send(new GetIdentityByIdQuery(id), ct)
                           ?? throw ElwarkIdentificationException.NotFound();

            var accountId = _identityService.GetAccountId();
            if (accountId != identity.AccountId)
                return StatusCode(403);

            await _mediator.Send(
                new SendConfirmationCodeCommand(
                    accountId,
                    identity.IdentityId,
                    identity.Identification is Identification.Email
                        ? new Notification.PrimaryEmail(identity.Identification.Value)
                        : identity.Notification,
                    ConfirmationType.ConfirmIdentity,
                    CultureInfo.CurrentCulture
                ),
                ct);

            return NoContent();
        }

        [HttpPut("{id}/confirm/{code}")]
        public async Task<ActionResult> ConfirmIdentityAsync(IdentityId id, long code, CancellationToken ct)
        {
            var confirmation = await _mediator.Send(
                new CheckConfirmationByCodeQuery(id, code, ConfirmationType.ConfirmIdentity), ct
            );

            await _mediator.Send(new ActivateIdentityCommand(confirmation.IdentityId), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.ConfirmationId), ct);

            return NoContent();
        }

        [HttpPut("{id}/notification/{type}")]
        public async Task<ActionResult> ChangeNotificationTypeAsync(IdentityId id, NotificationType type,
            CancellationToken ct)
        {
            await _mediator.Send(new ChangeNotificationTypeCommand(id, type), ct);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIdentityAsync(IdentityId id, CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new DeleteIdentityCommand(accountId, id), ct);

            return NoContent();
        }
    }
}