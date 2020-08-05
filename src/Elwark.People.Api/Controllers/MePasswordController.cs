using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Models.Requests;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Domain.Exceptions;
using Elwark.People.Shared.Primitives;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("accounts/me/password"), ApiController]
    public class MePasswordController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IMediator _mediator;

        public MePasswordController(IMediator mediator, IIdentityService identityService)
        {
            _mediator = mediator;
            _identityService = identityService;
        }

        [HttpGet, Authorize(Policy = Policy.Account)]
        public async Task<ActionResult<bool>> IsAvailablePasswordAsync(CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var result = await _mediator.Send(new CheckPasswordAvailabilityQuery(accountId), ct);

            return Ok(result);
        }

        [HttpPost("code"), Authorize(Policy = Policy.Account)]
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

        [HttpPost, Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> CreatePasswordAsync([FromBody] CreatePasswordRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            var isPassword = await _mediator.Send(new CheckPasswordAvailabilityQuery(accountId), ct);

            if (isPassword)
                throw new ElwarkPasswordException(PasswordError.AlreadySet);

            var identity = _identityService.GetIdentityId();

            var confirmation = await _mediator.Send(
                new CheckConfirmationByCodeQuery(identity, request.Code, ConfirmationType.UpdatePassword),
                ct);

            await _mediator.Send(new UpdatePasswordCommand(accountId, null, request.Password), ct);
            await _mediator.Send(new DeleteConfirmationCommand(confirmation.ConfirmationId), ct);

            return NoContent();
        }

        [HttpPut, Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordRequest request,
            CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new UpdatePasswordCommand(accountId, request.Current, request.Password), ct);

            return NoContent();
        }
    }
}