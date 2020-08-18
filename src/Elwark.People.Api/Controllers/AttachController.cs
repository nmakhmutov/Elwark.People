using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Commands.AttachIdentity;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Api.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("accounts"), ApiController]
    public class AttachController : ControllerBase
    {
        private readonly IIdentityService _identityService;
        private readonly IMediator _mediator;

        public AttachController(IIdentityService identityService, IMediator mediator)
        {
            _identityService = identityService;
            _mediator = mediator;
        }

        [HttpPost("{id}/attach/google"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachGoogleAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachGoogleIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("{id}/attach/facebook"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachFacebookAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachFacebookIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("{id}/attach/microsoft"), Authorize(Policy = Policy.Identity)]
        public async Task<ActionResult> AttachMicrosoftAsync([FromRoute] AccountId id,
            [FromBody] AttachIdentityRequest request, CancellationToken ct)
        {
            await _mediator.Send(new AttachMicrosoftIdentityCommand(id, request.AccessToken), ct);
            return NoContent();
        }

        [HttpPost("me/attach/email"), Authorize(Policy = Policy.Account)]
        public async Task<ActionResult> AttachEmailIdentity([FromBody] Identification.Email email, CancellationToken ct)
        {
            var accountId = _identityService.GetAccountId();
            await _mediator.Send(new AttachEmailIdentityCommand(accountId, email), ct);

            return NoContent();
        }
    }
}