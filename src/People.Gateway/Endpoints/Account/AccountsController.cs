using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using People.Gateway.Endpoints.Account.Model;
using People.Gateway.Endpoints.Account.Request;
using People.Gateway.Infrastructure;
using People.Gateway.Infrastructure.Identity;
using People.Grpc.Common;
using People.Grpc.Gateway;
using ConfirmRequest = People.Grpc.Gateway.ConfirmRequest;
using CreatePasswordRequest = People.Gateway.Endpoints.Account.Request.CreatePasswordRequest;
using UpdateAccountRequest = People.Gateway.Requests.UpdateAccountRequest;
using UpdatePasswordRequest = People.Gateway.Endpoints.Account.Request.UpdatePasswordRequest;

namespace People.Gateway.Endpoints.Account;

[ApiController, Route("accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IIdentityService _identity;
    private readonly PeopleService.PeopleServiceClient _people;

    public AccountsController(IIdentityService identity, PeopleService.PeopleServiceClient people)
    {
        _identity = identity;
        _people = people;
    }

    [HttpGet("{id:long}"), Authorize(Policy = Policy.RequireCommonAccess)]
    public async Task<IActionResult> GetAsync(long id, CancellationToken ct)
    {
        var account = await _people.GetAccountAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return Ok(account.ToSummary());
    }

    [HttpGet("me"), Authorize(Policy = Policy.RequireAccountId)]
    public async Task<IActionResult> GetAsync(CancellationToken ct)
    {
        var id = _identity.GetAccountId();
        var account = await _people.GetAccountAsync(id, cancellationToken: ct);

        return Ok(account.ToDetails());
    }

    [HttpPut("me"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        var account = await _people.UpdateAccountAsync(
            new Grpc.Gateway.UpdateAccountRequest
            {
                Id = _identity.GetAccountId(),
                Language = request.Language,
                Nickname = request.Nickname,
                PreferNickname = request.PreferNickname,
                TimeZone = request.TimeZone,
                WeekStart = request.WeekStart.ToGrpc(),
                CountryCode = request.CountryCode,
                FirstName = string.IsNullOrEmpty(request.FirstName) ? null : request.FirstName,
                LastName = string.IsNullOrEmpty(request.LastName) ? null : request.LastName,
                DateFormat = request.DateFormat,
                TimeFormat = request.TimeFormat
            },
            cancellationToken: ct
        );

        return Ok(account.ToDetails());
    }

    [HttpPut("me/connections"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> ConfirmAsync([FromBody] ConfirmConnectionRequest request, CancellationToken ct)
    {
        var account = await _people.ConfirmConnectionAsync(
            new ConfirmRequest
            {
                Id = _identity.GetAccountId(),
                Confirm = new Confirm
                {
                    Code = request.ConfirmationCode,
                    Token = request.ConfirmationToken
                },
                Identity = new Identity
                {
                    Type = request.Type,
                    Value = request.Value
                }
            },
            cancellationToken: ct
        );

        return Ok(account.ToDetails());
    }

    [HttpPut("me/connections/primary-email"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> UpdateAsync([FromBody] UpdatePrimaryEmailRequest request, CancellationToken ct)
    {
        var account = await _people.ChangePrimaryEmailAsync(
            new ChangePrimaryEmailRequest
            {
                Id = _identity.GetAccountId(),
                Email = request.Email
            },
            cancellationToken: ct
        );

        return Ok(account.ToDetails());
    }

    [HttpDelete("me/connections/{type}/{value}"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> DeleteIdentityAsync(IdentityType type, string value, CancellationToken ct)
    {
        var account = await _people.DeleteConnectionAsync(new DeleteConnectionRequest
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

        return Ok(account.ToDetails());
    }

    [HttpPost("me/passwords"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<ActionResult> CreatePasswordAsync([FromBody] CreatePasswordRequest request, CancellationToken ct)
    {
        var account = await _people.CreatePasswordAsync(
            new Grpc.Gateway.CreatePasswordRequest
            {
                Id = _identity.GetAccountId(),
                Password = request.Password,
                Confirm = new Confirm
                {
                    Token = request.Token,
                    Code = request.Code
                }
            },
            cancellationToken: ct
        );

        return Ok(account.ToDetails());
    }

    [HttpPut("me/passwords"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<ActionResult> UpdatePasswordAsync([FromBody] UpdatePasswordRequest request, CancellationToken ct)
    {
        await _people.UpdatePasswordAsync(new Grpc.Gateway.UpdatePasswordRequest
            {
                Id = _identity.GetAccountId(),
                NewPassword = request.NewPassword,
                OldPassword = request.OldPassword
            },
            cancellationToken: ct
        );

        return NoContent();
    }

    [HttpPost("me/confirmations"), Authorize(Policy = Policy.RequireProfileAccess)]
    public async Task<IActionResult> CreateAsync(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CreateConfirmationRequest? request,
        CancellationToken ct)
    {
        var identity = request is null
            ? null
            : new Identity
            {
                Type = request.Type,
                Value = request.Value
            };

        var confirmation = await _people.SendConfirmationCodeAsync(
            new ConfirmationCodeRequest
            {
                Id = _identity.GetAccountId(),
                Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Identity = identity
            },
            cancellationToken: ct
        );

        return Ok(new ConfirmationModel(confirmation.Token));
    }
}
