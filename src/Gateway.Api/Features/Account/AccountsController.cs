using System.Threading;
using System.Threading.Tasks;
using Gateway.Api.Infrastructure;
using Gateway.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Grpc.Common;
using People.Grpc.Gateway;

namespace Gateway.Api.Features.Account;

[ApiController, Route("accounts")]
public sealed partial class AccountsController : ControllerBase
{
    private readonly IIdentityService _identity;
    private readonly PeopleService.PeopleServiceClient _people;

    public AccountsController(IIdentityService identity, PeopleService.PeopleServiceClient people)
    {
        _identity = identity;
        _people = people;
    }

    [HttpGet("me"), Authorize(Policy = Policy.RequireAccountId)]
    public async Task<ActionResult> GetAsync(CancellationToken ct)
    {
        var account = await _people.GetProfileAsync(_identity.GetAccountId(), cancellationToken: ct);

        return Ok(ToAccount(account));
    }

    [HttpGet("{id:long}"), Authorize(Policy = Policy.RequireCommonAccess)]
    public async Task<ActionResult> GetAsync(long id, CancellationToken ct)
    {
        var account = await _people.GetProfileAsync(new AccountIdValue { Value = id }, cancellationToken: ct);

        return Ok(ToAccount(account));
    }
}
