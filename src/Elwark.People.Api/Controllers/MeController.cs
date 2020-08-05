using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Api.Application.Commands;
using Elwark.People.Api.Application.Models.Requests;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Infrastructure.Services.Identity;
using Elwark.People.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("accounts/me"), ApiController]
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
        public async Task<ActionResult<AccountResponse>> GetMeAsync(CancellationToken ct)
        {
            var id = _identityService.GetAccountId();
            var result = await _mediator.Send(new GetAccountByIdQuery(id), ct)
                         ?? throw ElwarkAccountException.NotFound(id);

            return Ok(result);
        }

        [HttpPut, Authorize(Policy = Policy.Account)]
        public async Task<ActionResult<AccountResponse>> UpdateMeAsync(UpdateAccountRequest request,
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
    }
}