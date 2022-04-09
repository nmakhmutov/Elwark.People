using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Gateway.Features.Account.Models;
using People.Gateway.Features.Account.Requests;
using People.Gateway.Infrastructure;
using People.Grpc.Common;
using People.Grpc.Gateway;

namespace People.Gateway.Features.Account;

[ApiController, Route("management/accounts"), Authorize(Policy = Policy.ManagementAccess)]
public sealed partial class ManagementController : ControllerBase
{
    private readonly PeopleManagement.PeopleManagementClient _management;

    public ManagementController(PeopleManagement.PeopleManagementClient management) =>
        _management = management;

    [HttpGet]
    public async Task<IActionResult> GetAsync([FromRoute] GetAccountsRequest request, CancellationToken ct)
    {
        var data = await _management.GetAccountsAsync(
            new ManagementAccountsRequest
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
        var account = await _management.GetAccountAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return Ok(ToAccount(account));
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateAsync([FromRoute] long id, [FromBody] Requests.UpdateAccountRequest request,
        CancellationToken ct)
    {
        var result = await _management.UpdateAccountAsync(
            new People.Grpc.Gateway.UpdateAccountRequest
            {
                Id = new AccountIdValue { Value = id },
                Language = request.Language,
                Nickname = request.Nickname,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferNickname = request.PreferNickname,
                Picture = request.Picture,
                CountryCode = request.CountryCode,
                TimeZone = request.TimeZone,
                FirstDayOfWeek = request.FirstDayOfWeek
            },
            cancellationToken: ct
        );

        return Ok(ToAccount(result));
    }

    [HttpPut("{id:long}/connections/{type}/{value}/confirm")]
    public async Task<IActionResult> ConfirmConnectionAsync([FromRoute] long id, [FromRoute] IdentityType type,
        [FromRoute] string value, CancellationToken ct)
    {
        var account = await _management.ConfirmConnectionAsync(
            new ConfirmManagementRequest
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

    [HttpPut("{id:long}/connections/{type}/{value}/confute")]
    public async Task<IActionResult> ConfuteConnectionAsync([FromRoute] long id, [FromRoute] IdentityType type,
        [FromRoute] string value, CancellationToken ct)
    {
        var account = await _management.ConfuteConnectionAsync(
            new ConfirmManagementRequest
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
        var account = await _management.DeleteConnectionAsync(
            new ConfirmManagementRequest
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
        var account = await _management.CreateRoleAsync(
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
        var account = await _management.DeleteRoleAsync(
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
    public async Task<IActionResult> BanAsync([FromRoute] long id, [FromBody] Requests.BanRequest request, CancellationToken ct)
    {
        var account = await _management.BanAsync(
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
        var account = await _management.UnbanAsync(
            new AccountIdValue { Value = id },
            cancellationToken: ct
        );

        return Ok(ToAccount(account));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] long id, CancellationToken ct)
    {
        await _management.DeleteAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return NoContent();
    }
}
