using System;
using System.Threading;
using System.Threading.Tasks;
using Elwark.People.Abstractions;
using Elwark.People.Api.Application.Models;
using Elwark.People.Api.Application.Queries.SignIn;
using Elwark.People.Api.Infrastructure;
using Elwark.People.Api.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Elwark.People.Api.Controllers
{
    [Route("[controller]"), ApiController, Authorize(Policy = Policy.Identity)]
    public class SignInController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SignInController(IMediator mediator) =>
            _mediator = mediator;

        [HttpPost]
        public async Task<ActionResult<SignInModel>> IndexAsync([FromBody] SignInRequest request, CancellationToken ct)
        {
            IRequest<SignInModel> query = request.Identification switch
            {
                Identification.Email email => new SignInByEmailQuery(email, request.Verifier),
                Identification.Facebook facebook => new SignInByFacebookQuery(facebook, request.Verifier),
                Identification.Google google => new SignInByGoogleQuery(google, request.Verifier),
                Identification.Microsoft microsoft => new SignInByMicrosoftQuery(microsoft, request.Verifier),
                _ => throw new NotImplementedException() 
            };

            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }
    }
}