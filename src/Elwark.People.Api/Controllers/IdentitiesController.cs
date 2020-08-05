using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models.Responses;
using Elwark.People.Api.Application.Queries;
using Elwark.People.Api.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController, Authorize(Policy = Policy.Identity)]
    public class IdentitiesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public IdentitiesController(IMediator mediator) =>
            _mediator = mediator;

        [HttpGet("{id}/status")]
        public async Task<ActionResult<IdentityActiveResponse>> CheckStatusAsync(IdentityId id, CancellationToken ct)
        {
            var result = await _mediator.Send(new CheckIdentityQuery(id), ct);

            return Ok(result);
        }
    }
}