using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Gateway.Mappes;
using People.Gateway.Requests;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Confirm = People.Gateway.Requests.ConfirmRequest;
using ConfirmRequest = People.Grpc.Gateway.ConfirmRequest;
using Identity = People.Grpc.Common.Identity;

namespace People.Gateway.Features.Profile
{
    [ApiController, Route("profiles/me"), Authorize(Policy = Policy.RequireProfileAccess)]
    public sealed partial class ProfilesController : ControllerBase
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
            var profile = await _client.GetProfileAsync(_identity.GetAccountId(), cancellationToken: ct);

            if (profile is null)
                return NotFound();

            return Ok(ToProfile(profile));
        }

        [HttpPut]
        public async Task<ActionResult> UpdateAsync(UpdateAccount request, CancellationToken ct)
        {
            var profile = await _client.UpdateProfileAsync(new UpdateProfileRequest
                {
                    Id = _identity.GetAccountId(),
                    Language = request.Language,
                    Nickname = request.Nickname,
                    PreferNickname = request.PreferNickname,
                    TimeZone = request.TimeZone,
                    FirstDayOfWeek = request.FirstDayOfWeek.ToGrpc(),
                    CountryCode = request.CountryCode,
                    FirstName = string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
                    LastName = string.IsNullOrEmpty(request.LastName) ? null : request.LastName
                },
                cancellationToken: ct
            );

            return Ok(ToProfile(profile));
        }

        [HttpPost("connections/{type}/{value}/confirm")]
        public async Task<ActionResult> ConfirmingIdentityAsync(IdentityType type, string value, CancellationToken ct)
        {
            var confirmation = await _client.ConfirmingConnectionAsync(
                new ConfirmingRequest
                {
                    Id = _identity.GetAccountId(),
                    Identity = new Identity
                    {
                        Type = type,
                        Value = value
                    },
                    Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                cancellationToken: ct
            );

            return Ok(new Confirming(confirmation.Id));
        }

        [HttpPut("connections/{type}/{value}/confirm")]
        public async Task<ActionResult> ConfirmIdentityAsync(IdentityType type, string value,
            [FromBody] Confirm request, CancellationToken ct)
        {
            var profile = await _client.ConfirmConnectionAsync(new ConfirmRequest
                {
                    Id = _identity.GetAccountId(),
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

            return Ok(ToProfile(profile));
        }

        [HttpPut("connections/email/{value}/primary")]
        public async Task<ActionResult> SetAsPrimaryAsync(string value, CancellationToken ct)
        {
            var profile = await _client.SetEmailAsPrimaryAsync(new SetEmailAsPrimaryRequest
                {
                    Id = _identity.GetAccountId(),
                    Email = value
                },
                cancellationToken: ct
            );

            return Ok(ToProfile(profile));
        }

        [HttpDelete("connections/{type}/{value}")]
        public async Task<ActionResult> DeleteIdentityAsync(IdentityType type, string value, CancellationToken ct)
        {
            var profile = await _client.DeleteConnectionAsync(new DeleteConnectionRequest
                {
                    Id = _identity.GetAccountId(),
                    Identity = new Identity
                    {
                        Type = type,
                        Value = value
                    }
                },
                cancellationToken: ct
            );

            return Ok(ToProfile(profile));
        }

        [HttpPost("password/confirm")]
        public async Task<ActionResult> CreatingPasswordAsync(CancellationToken ct)
        {
            var confirmation = await _client.CreatingPasswordAsync(
                new CreatingPasswordRequest
                {
                    Id = _identity.GetAccountId(),
                    Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
                },
                cancellationToken: ct
            );

            return Ok(new Confirming(confirmation.Id));
        }

        [HttpPost("password")]
        public async Task<ActionResult> CreatePasswordAsync([FromBody] CreatePassword request, CancellationToken ct)
        {
            var profile = await _client.CreatePasswordAsync(
                new CreatePasswordRequest
                {
                    Id = _identity.GetAccountId(),
                    Confirm = new Grpc.Common.Confirm
                    {
                        Id = request.Id,
                        Code = request.Code
                    },
                    Password = request.Password
                },
                cancellationToken: ct
            );

            return Ok(ToProfile(profile));
        }

        [HttpPut("password")]
        public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePassword request, CancellationToken ct)
        {
            await _client.UpdatePasswordAsync(new UpdatePasswordRequest
                {
                    Id = _identity.GetAccountId(),
                    NewPassword = request.NewPassword,
                    OldPassword = request.OldPassword
                },
                cancellationToken: ct
            );

            return NoContent();
        }
    }
}
