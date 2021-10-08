using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gateway.Api.Features.Management.Models;
using Gateway.Api.Features.Management.Requests;
using Gateway.Api.Infrastructure;
using Gateway.Api.Mappes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Ban = Gateway.Api.Features.Management.Models.Ban;
using Connection = Gateway.Api.Features.Management.Models.Connection;

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
        var account = await _management.GetAccountAsync(new AccountId { Value = id }, cancellationToken: ct);

        return Ok(
            new AccountModel(
                account.Id.Value,
                account.Name.Nickname,
                account.Name.PreferNickname,
                account.Name.FirstName,
                account.Name.LastName,
                account.Name.FullName,
                account.Language,
                account.Picture,
                account.CountryCode,
                account.TimeZone,
                account.FirstDayOfWeek.FromGrpc(),
                account.Ban is null ? null : new Ban(account.Ban.Reason, account.Ban.ExpiresAt.ToDateTime()),
                account.IsPasswordAvailable,
                account.CreatedAt.ToDateTime(),
                account.LastSignIn.ToDateTime(),
                account.Roles,
                account.Connections.Select(ToGrpc)
            )
        );
    }

    private static Connection ToGrpc(ManagementAccountReply.Types.Connection connection) =>
        connection.ConnectionTypeCase switch
        {
            ManagementAccountReply.Types.Connection.ConnectionTypeOneofCase.Email =>
                new Models.EmailConnection(
                    connection.Type,
                    connection.Value,
                    connection.CreatedAt.ToDateTime(),
                    connection.ConfirmedAt?.ToDateTime(),
                    connection.Email.IsPrimary
                ),

            ManagementAccountReply.Types.Connection.ConnectionTypeOneofCase.Social =>
                new Models.SocialConnection(
                    connection.Type,
                    connection.Value,
                    connection.CreatedAt.ToDateTime(),
                    connection.ConfirmedAt?.ToDateTime(),
                    connection.Social.FirstName,
                    connection.Social.LastName
                ),

            _ => throw new ArgumentOutOfRangeException(nameof(connection), connection, null)
        };
}
