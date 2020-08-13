using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AccountsController(IMediator mediator) =>
            _mediator = mediator;

        [HttpGet("{id}"), Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<AccountModel>> GetAccountAsync(AccountId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetAccountByIdQuery(id), ct)
                         ?? throw ElwarkAccountException.NotFound(id);

            return Ok(result);
        }

        [HttpGet("{id}/ban"), Authorize(Policy = Policy.Common)]
        public async Task<ActionResult<BanModel>> GetBanAsync(AccountId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new GetBanQuery(id), ct);

            return Ok(result);
        }
    }
}