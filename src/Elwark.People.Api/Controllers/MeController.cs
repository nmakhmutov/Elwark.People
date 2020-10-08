using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Commands.AttachIdentity;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Api.Requests;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController]
    public class MeController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IMediator _mediator;

        public MeController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }

        [HttpGet, Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<AccountModel>> GetMeAsync(CancellationToken ct)
        {
            var id = _identityService.GetAccountId();
            var result = await _mediator.Send(new GetAccountByIdQuery(id), ct)
                         ?? throw ElwarkAccountException.NotFound(id);

            return Ok(result);
        }

        [HttpPut, Authorize(Policy = Policy.Account)]
        public async Task<ActionResult<AccountModel>> UpdateMeAsync(UpdateAccountRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();

            var command = new UpdateAccountCommand(
                accountId,
                request.Language,
                request.Gender,
                request.Birthdate,
                request.FirstName,
                request.LastName,
                request.Nickname,
                request.Picture,
                request.Timezone,
                request.CountryCode,
                request.City,
                request.Bio,
                request.Links
            );

            await _mediator.Send(command, ct);
            var account = await _mediator.Send(new GetAccountByIdQuery(accountId), ct);

            return Ok(account);
        }

        [HttpPut("picture"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> UpdatePictureAsync([FromBody] UpdatePictureRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new ChangePictureCommand(accountId, request.Picture), ct);

            return NoContent();
        }

        [HttpPost("email"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> AttachEmailIdentity([FromBody] Identification.Email email, CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new AttachEmailIdentityCommand(accountId, email), ct);

            return NoContent();
        }

        [HttpGet("identities"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult<IEnumerable<IdentityModel>>> GetIdentitiesAsync(CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var result = await _mediator.Send(new GetIdentitiesQuery(accountId), ct);

            return Ok(result);
        }

        [HttpPost("identities/{id}/confirm"), Authorize(Policy = Policy.Account)]
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

        [HttpPut("identities/{id}/confirm/{code}"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> ConfirmIdentityAsync(IdentityId id, long code, CancellationToken ct)
        {
            var confirmation = await _mediator.Send(
                new CheckConfirmationQuery(id, ConfirmationType.ConfirmIdentity, code), ct
            );

            await _mediator.Send(new ActivateIdentityCommand(confirmation.IdentityId), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.IdentityId, confirmation.Type), ct);

            return NoContent();
        }

        [HttpPut("identities/{id}/notification/{type}"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> ChangeNotificationTypeAsync(IdentityId id, NotificationType type,
            CancellationToken ct)
        {
            await _mediator.Send(new ChangeNotificationTypeCommand(id, type), ct);

            return NoContent();
        }

        [HttpDelete("identities/{id}"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> DeleteIdentityAsync(IdentityId id, CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new DeleteIdentityCommand(accountId, id), ct);

            return NoContent();
        }

        [HttpGet("password"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult<bool>> IsAvailablePasswordAsync(CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var result = await _mediator.Send(new CheckPasswordAvailabilityQuery(accountId), ct);

            return Ok(result);
        }

        [HttpPost("password/code"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> CreatePasswordConfirmationAsync(CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var result = await _mediator.Send(new CheckPasswordAvailabilityQuery(accountId), ct);

            if (result)
                throw new ElwarkPasswordException(PasswordError.AlreadySet);

            var identity = _identityService.GetIdentityId();
            await _mediator.Send(
                new SendConfirmationCodeCommand(
                    accountId,
                    identity,
                    new Notification.NoneNotification(),
                    ConfirmationType.UpdatePassword,
                    CultureInfo.CurrentCulture
                ),
                ct);

            return NoContent();
        }

        [HttpPost("password"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> CreatePasswordAsync([FromBody] CreatePasswordRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var isPassword = await _mediator.Send(new CheckPasswordAvailabilityQuery(accountId), ct);

            if (isPassword)
                throw new ElwarkPasswordException(PasswordError.AlreadySet);

            var identityId = _identityService.GetIdentityId();

            var confirmation = await _mediator.Send(
                new CheckConfirmationQuery(identityId, ConfirmationType.UpdatePassword, request.Code), ct
            );

            await _mediator.Send(new UpdatePasswordCommand(accountId, null, request.Password), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.IdentityId, confirmation.Type), ct);

            return NoContent();
        }

        [HttpPut("password"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new UpdatePasswordCommand(accountId, request.Current, request.Password), ct);

            return NoContent();
        }
    }
}