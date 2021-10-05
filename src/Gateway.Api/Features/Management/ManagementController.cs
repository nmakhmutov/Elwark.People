using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Api.Features.Management.Models;
using Gateway.Api.Features.Management.Requests;
using Gateway.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Grpc.Gateway;

namespace Gateway.Api.Features.Management;

[ApiController, Route("management/accounts"), Authorize(Policy = Policy.ManagementAccess)]
public sealed class ManagementController : ControllerBase
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

        var result = new PageResponse<AccountModel>(
            data.Topics.Select(x => new AccountModel(
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
}
