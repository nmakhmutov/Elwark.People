using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Endpoints.Management.Request;
using People.Gateway.Infrastructure;
using People.Grpc.Common;
using People.Grpc.Gateway;
using AccountSummary = People.Gateway.Endpoints.Management.Model.AccountSummary;
using BanRequest = People.Gateway.Endpoints.Management.Request.BanRequest;
using UpdateAccountRequest = People.Gateway.Endpoints.Management.Request.UpdateAccountRequest;

namespace People.Gateway.Endpoints.Management;

[ApiController, Route("management/accounts"), Authorize(Policy = Policy.ManagementAccess)]
public sealed partial class ManagementController : ControllerBase
{
    private readonly PeopleService.PeopleServiceClient _people;

    public ManagementController(PeopleService.PeopleServiceClient people) =>
        _people = people;

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] GetAccountsRequest request, CancellationToken ct)
    {
        var data = await _people.GetAccountsAsync(
            new AccountsRequest
            {
                Limit = request.Limit,
                Page = request.Page
            },
            cancellationToken: ct
        );

        var result = new PageResponse<AccountSummary>(
            data.Topics.Select(x =>
                new AccountSummary(
                    x.Id.Value,
                    x.Name.FirstName,
                    x.Name.LastName,
                    x.Name.Nickname,
                    x.Picture,
                    x.CountryCode,
                    x.TimeZone,
                    x.CreatedAt.ToDateTime()
                )
            ),
            data.Pages,
            data.Count
        );

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetAsync([FromRoute] long id, CancellationToken ct)
    {
        var account = await _people.GetAccountAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return Ok(ToAccount(account));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateAsync([FromRoute] long id, [FromBody] UpdateAccountRequest request,
        CancellationToken ct)
    {
        var result = await _people.UpdateAccountAsync(
            new People.Grpc.Gateway.UpdateAccountRequest
            {
                Id = new AccountIdValue { Value = id },
                Language = request.Language,
                Nickname = request.Nickname,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferNickname = request.PreferNickname,
                CountryCode = request.CountryCode,
                TimeZone = request.TimeZone,
                WeekStart = request.WeekStart,
                DateFormat = request.DateFormat,
                TimeFormat = request.TimeFormat
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(result));
    }

    [HttpPut("{id:long}/connections/{type}/{value}/confirm")]
    public async Task<IActionResult> ConfirmConnectionAsync([FromRoute] long id, [FromRoute] IdentityType type,
        [FromRoute] string value, CancellationToken ct)
    {
        var account = await _people.ConfirmConnectionAsync(
            new ConfirmRequest
            {
                Id = new AccountIdValue { Value = id },
                Confirm = null,
                Identity = new Identity
                {
                    Type = type,
                    Value = value
                }
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpPut("{id:long}/connections/{type}/{value}/confute")]
    public async Task<IActionResult> ConfuteConnectionAsync([FromRoute] long id, [FromRoute] IdentityType type,
        [FromRoute] string value, CancellationToken ct)
    {
        var account = await _people.ConfuteConnectionAsync(
            new ConfuteRequest
            {
                Id = new AccountIdValue { Value = id },
                Identity = new Identity
                {
                    Type = type,
                    Value = value
                }
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpDelete("{id:long}/connections/{type}/{value}")]
    public async Task<IActionResult> DeleteConnectionAsync([FromRoute] long id, [FromRoute] IdentityType type,
        [FromRoute] string value, CancellationToken ct)
    {
        var account = await _people.DeleteConnectionAsync(
            new DeleteConnectionRequest
            {
                Id = new AccountIdValue { Value = id },
                Identity = new Identity
                {
                    Type = type,
                    Value = value
                }
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpPost("{id:long}/roles/{role}")]
    public async Task<IActionResult> CreateRoleAsync([FromRoute] long id, [FromRoute] string role, CancellationToken ct)
    {
        var account = await _people.CreateRoleAsync(
            new RoleRequest
            {
                Id = new AccountIdValue { Value = id },
                Role = role
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpDelete("{id:long}/roles/{role}")]
    public async Task<IActionResult> DeleteRoleAsync([FromRoute] long id, [FromRoute] string role, CancellationToken ct)
    {
        var account = await _people.DeleteRoleAsync(
            new RoleRequest
            {
                Id = new AccountIdValue { Value = id },
                Role = role
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpPost("{id:long}/ban")]
    public async Task<IActionResult> BanAsync([FromRoute] long id, [FromBody] BanRequest request, CancellationToken ct)
    {
        var account = await _people.BanAsync(
            new People.Grpc.Gateway.BanRequest
            {
                Id = new AccountIdValue { Value = id },
                Reason = request.Reason,
                ExpiredAt = request.ExpiredAt?.ToTimestamp()
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpDelete("{id:long}/ban")]
    public async Task<IActionResult> UnbanAsync([FromRoute] long id, CancellationToken ct)
    {
        var account = await _people.UnbanAsync(
            new AccountIdValue { Value = id },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _people.DeleteAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return NoContent();
    }
}
