using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Gateway.Mappers;
using People.Gateway.Models;
using People.Grpc.Common;

namespace People.Gateway.Controllers
{
    [ApiController, Route("accounts")]
    public sealed class AccountsController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _client;
        private readonly IIdentityService _identity;

        public AccountsController(Grpc.Gateway.Gateway.GatewayClient client, IIdentityService identity)
        {
            _client = client;
            _identity = identity;
        }

        [HttpGet("me"), Authorize(Policy = Policy.RequireAccountId)]
        public async Task<ActionResult> MeAsync(CancellationToken ct)
        {
            var id = _identity.GetAccountId();
            var account = await _client.GetAccountAsync(new AccountId {Value = id}, cancellationToken: ct);

            return Ok(
                new Account(
                    account.Id.Value,
                    account.Name.Nickname,
                    account.Name.FirstName,
                    account.Name.LastName,
                    account.Name.FullName,
                    account.Profile.Language,
                    account.Profile.Gender,
                    account.Profile.Birthday?.ToDateTime(),
                    account.Profile.Bio,
                    account.Profile.Picture,
                    account.Address.ToAddress(),
                    account.Timezone.ToTimezone(),
                    account.IsBanned
                )
            );
        }

        [HttpPost("{id:long}/email"), Authorize(Policy = Policy.RequireCommonAccess)]
        public async Task<ActionResult> SendEmailAsync([FromRoute] long id, [FromBody] SendEmailRequest request,
            CancellationToken ct)
        {
            var callOptions = new CallOptions(cancellationToken: ct);

            await _client.SendEmailAsync(new Grpc.Gateway.SendEmailRequest
                {
                    Id = id,
                    Body = request.Body,
                    Subject = request.Subject
                },
                callOptions
            );

            return Accepted();
        }

        public sealed record SendEmailRequest([Required] string Subject, [Required] string Body);
    }
}
