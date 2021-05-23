using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Gateway.Mappers;
using People.Gateway.Requests;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Confirming = People.Gateway.Models.Confirming;
using Confirm = People.Gateway.Requests.ConfirmRequest;
using Identity = People.Grpc.Common.Identity;

namespace People.Gateway.Controllers
{
    [ApiController, Route("profiles/me"), Authorize(Policy = Policy.RequireProfileAccess)]
    public sealed class ProfilesController : ControllerBase
    {
        private readonly Grpc.Gateway.Gateway.GatewayClient _client;
        private readonly IIdentityService _identity;

        public ProfilesController(Grpc.Gateway.Gateway.GatewayClient client, IIdentityService identity)
        {
            _client = client;
            _identity = identity;
        }

        [HttpGet]
        public async Task<ActionResult> GetAsync(CancellationToken ct)
        {
            var id = _identity.GetAccountId();
            var callOption = new CallOptions(cancellationToken: ct);

            var profile = await _client.GetProfileAsync(new AccountId {Value = id}, callOption);
            if (profile is null)
                return NotFound();

            return Ok(profile.ToProfile());
        }

        [HttpPut]
        public async Task<ActionResult> UpdateAsync(UpdateAccount request, CancellationToken ct)
        {
            var profile = await _client.UpdateProfileAsync(new UpdateProfileRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Bio = string.IsNullOrEmpty(request.Bio) ? null : request.Bio,
                    DateOfBirth = request.DateOfBirth.ToTimestamp(),
                    Gender = request.Gender,
                    Language = request.Language,
                    Nickname = request.Nickname,
                    Timezone = request.Timezone,
                    CityName = string.IsNullOrEmpty(request.CityName) ? null : request.CityName,
                    CountryCode = request.CountryCode,
                    FirstName = string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
                    LastName = string.IsNullOrEmpty(request.LastName) ? null : request.LastName
                },
                new CallOptions(cancellationToken: ct)
            );

            return Ok(profile.ToProfile());
        }

        [HttpPost("identities/{type}/value/{value}/confirm")]
        public async Task<ActionResult> ConfirmingIdentityAsync([FromRoute] IdentityType type, [FromRoute] string value,
            CancellationToken ct)
        {
            var confirmation = await _client.ConfirmingIdentityAsync(
                new ConfirmingRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Identity = new Identity
                    {
                        Type = type,
                        Value = value
                    }
                },
                cancellationToken: ct
            );

            return Ok(new Confirming(confirmation.Id));
        }

        [HttpPut("identities/{type}/value/{value}/confirm")]
        public async Task<ActionResult> ConfirmIdentityAsync([FromRoute] IdentityType type, [FromRoute] string value,
            [FromBody] Confirm request, CancellationToken ct)
        {
            var profile = await _client.ConfirmIdentityAsync(new Grpc.Gateway.ConfirmRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Confirm = new Grpc.Common.Confirm
                    {
                        Code = request.Code,
                        Id = request.Id
                    },
                    Identity = new Identity
                    {
                        Type = type,
                        Value = value
                    }
                },
                cancellationToken: ct
            );

            return Ok(profile.ToProfile());
        }

        [HttpPut("identities/email")]
        public async Task<ActionResult> UpdateIdentityAsync([FromBody] ChangeEmailType request, CancellationToken ct)
        {
            var profile = await _client.ChangeEmailTypeAsync(new ChangeEmailTypeRequest
                {
                    Email = request.Email,
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Type = request.Type
                },
                cancellationToken: ct
            );

            return Ok(profile.ToProfile());
        }

        [HttpDelete("identities/{type}/value/{value}")]
        public async Task<ActionResult> DeleteIdentityAsync([FromRoute] IdentityType type, [FromRoute] string value,
            CancellationToken ct)
        {
            var profile = await _client.DeleteIdentityAsync(new DeleteIdentityRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Identity = new Identity
                    {
                        Type = type,
                        Value = value
                    }
                },
                new CallOptions(cancellationToken: ct)
            );

            return Ok(profile.ToProfile());
        }

        [HttpPost("password/confirm")]
        public async Task<ActionResult> CreatingPasswordAsync(CancellationToken ct)
        {
            var confirmation = await _client.CreatingPasswordAsync(new AccountId
                {
                    Value = _identity.GetAccountId()
                },
                new CallOptions(cancellationToken: ct)
            );

            return Ok(new Confirming(confirmation.Id));
        }

        [HttpPost("password")]
        public async Task<ActionResult> CreatePasswordAsync([FromBody] CreatePassword request, CancellationToken ct)
        {
            var profile = await _client.CreatePasswordAsync(
                new CreatePasswordRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    Confirm = new Grpc.Common.Confirm
                    {
                        Id = request.Id,
                        Code = request.Code
                    },
                    Password = request.Password
                },
                new CallOptions(cancellationToken: ct)
            );

            return Ok(profile.ToProfile());
        }

        [HttpPut("password")]
        public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePassword request, CancellationToken ct)
        {
            await _client.UpdatePasswordAsync(new UpdatePasswordRequest
                {
                    Id = new AccountId
                    {
                        Value = _identity.GetAccountId()
                    },
                    NewPassword = request.NewPassword,
                    OldPassword = request.OldPassword
                },
                new CallOptions(cancellationToken: ct)
            );

            return NoContent();
        }
    }
}
