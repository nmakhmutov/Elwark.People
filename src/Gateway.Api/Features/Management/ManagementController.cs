using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Api.Features.Management.Models;
using Gateway.Api.Features.Management.Requests;
using Gateway.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Grpc.Common;
using People.Grpc.Gateway;
using UpdateAccountRequest = Gateway.Api.Features.Management.Requests.UpdateAccountRequest;

namespace Gateway.Api.Features.Management;

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
    public async Task<IActionResult> UpdateAsync([FromRoute] long id, [FromBody] UpdateAccountRequest request,
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
}
