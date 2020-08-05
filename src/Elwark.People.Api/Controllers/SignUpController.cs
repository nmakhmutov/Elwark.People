using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Commands.SignUp;
using Elwark.People.Api.Application.Models.Requests;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Infrastructure;
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
        public async Task<ActionResult<SignUpResponse>> SignUpAsync([FromBody] SignUpByEmailRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByEmailCommand(request.Email, request.Password),
                ct
            );

            await _mediator.Send(
                new SendConfirmationsAfterSignUpCommand(
                    result.AccountId,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture,
                    result.Identities
                ),
                ct
            );

            return Ok(result);
        }

        [HttpPost("google")]
        public async Task<ActionResult<SignUpResponse>> SignUpAsync([FromBody] SignUpByGoogleRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByGoogleCommand(request.Google, request.Email, request.AccessToken),
                ct
            );

            await _mediator.Send(
                new SendConfirmationsAfterSignUpCommand(
                    result.AccountId,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture,
                    result.Identities
                ),
                ct
            );

            return Ok(result);
        }

        [HttpPost("facebook")]
        public async Task<ActionResult<SignUpResponse>> SignUpAsync([FromBody] SignUpByFacebookRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByFacebookCommand(request.Facebook, request.Email, request.AccessToken),
                ct
            );

            await _mediator.Send(
                new SendConfirmationsAfterSignUpCommand(
                    result.AccountId,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture,
                    result.Identities
                ),
                ct
            );

            return Ok(result);
        }

        [HttpPost("microsoft")]
        public async Task<ActionResult<SignUpResponse>> SignUpAsync([FromBody] SignUpByMicrosoftRequest request,
            CancellationToken ct)
        {
            var result = await _mediator.Send(
                new SignUpByMicrosoftCommand(request.Microsoft, request.Email, request.AccessToken),
                ct
            );

            await _mediator.Send(
                new SendConfirmationsAfterSignUpCommand(
                    result.AccountId,
                    request.ConfirmationUrl,
                    CultureInfo.CurrentCulture,
                    result.Identities
                ),
                ct
            );

            return Ok(result);
        }
    }
}