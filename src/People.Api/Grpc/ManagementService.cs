using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Commands.BanAccount;
using People.Api.Application.Commands.ConfirmConnection;
using People.Api.Application.Commands.ConfuteConnection;
using People.Api.Application.Commands.CreateRole;
using People.Api.Application.Commands.DeleteAccount;
using People.Api.Application.Commands.DeleteConnection;
using People.Api.Application.Commands.DeleteRole;
using People.Api.Application.Commands.UnbanAccount;
using People.Api.Application.Commands.UpdateProfile;
using People.Api.Application.Queries.GetAccountById;
using People.Api.Application.Queries.GetAccounts;
using People.Api.Mappers;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Connection = People.Domain.Aggregates.AccountAggregate.Identities.Connection;
using EmailConnection = People.Domain.Aggregates.AccountAggregate.Identities.EmailConnection;

namespace People.Api.Grpc;

internal sealed class ManagementService : PeopleManagement.PeopleManagementBase
{
    private readonly IMediator _mediator;

    public ManagementService(IMediator mediator) =>
        _mediator = mediator;

    public override async Task<ManagementPageAccountsReply> GetAccounts(ManagementAccountsRequest request,
        ServerCallContext context)
    {
        var query = new GetAccountsQuery(request.Page, request.Limit);
        var (items, pages, count) = await _mediator.Send(query, context.CancellationToken);

        return new ManagementPageAccountsReply
        {
            Count = count,
            Pages = pages,
            Topics =
            {
                items.Select(x => new ManagementPageAccountsReply.Types.Account
                {
                    Id = x.AccountId,
                    Language = x.Language.ToString(),
                    Name = x.Name,
                    Picture = x.Picture.ToString(),
                    CountryCode = x.CountryCode.ToString(),
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    TimeZone = x.TimeZone
                })
            }
        };
    }

    public override Task<ManagementAccountReply> GetAccount(AccountIdValue request, ServerCallContext context) =>
        GetAccountAsync(request, context.CancellationToken);

    public override async Task<ManagementAccountReply> UpdateAccount(UpdateAccountRequest request,
        ServerCallContext context)
    {
        var command = new UpdateProfileCommand(
            request.Id,
            request.FirstName,
            request.LastName,
            request.Nickname,
            request.PreferNickname,
            request.Language,
            request.TimeZone,
            request.FirstDayOfWeek.FromGrpc(),
            request.CountryCode
        );

        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> ConfirmConnection(ConfirmManagementRequest request,
        ServerCallContext context)
    {
        var command = new ConfirmConnectionCommand(request.Id, request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> ConfuteConnection(ConfirmManagementRequest request,
        ServerCallContext context)
    {
        var command = new ConfuteConnectionCommand(request.Id, request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> DeleteConnection(ConfirmManagementRequest request,
        ServerCallContext context)
    {
        var command = new DeleteConnectionCommand(request.Id, request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> CreateRole(RoleRequest request, ServerCallContext context)
    {
        var command = new CreateRoleCommand(request.Id, request.Role);
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> DeleteRole(RoleRequest request, ServerCallContext context)
    {
        var command = new DeleteRoleCommand(request.Id, request.Role);
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> Ban(BanRequest request, ServerCallContext context)
    {
        var command = new BanAccountCommand(request.Id, request.Reason, request.ExpiredAt?.ToDateTime());
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request.Id, context.CancellationToken);
    }

    public override async Task<ManagementAccountReply> Unban(AccountIdValue request, ServerCallContext context)
    {
        var command = new UnbanAccountCommand(request);
        await _mediator.Send(command, context.CancellationToken);

        return await GetAccountAsync(request, context.CancellationToken);
    }

    public override async Task<Empty> Delete(AccountIdValue request, ServerCallContext context)
    {
        var command = new DeleteAccountCommand(request);
        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    private static ManagementAccountReply ToGrpc(Account account) =>
        new()
        {
            Ban = account.Ban.ToGrpc(),
            Connections = { account.Connections.Select(ToGrpc) },
            Id = account.Id,
            Language = account.Language.ToString(),
            Name = account.Name,
            Picture = account.Picture.ToString(),
            Roles = { account.Roles },
            CountryCode = account.CountryCode.ToString(),
            CreatedAt = account.CreatedAt.ToTimestamp(),
            TimeZone = account.TimeZone,
            IsPasswordAvailable = account.IsPasswordAvailable(),
            LastSignIn = account.LastSignIn.ToTimestamp(),
            FirstDayOfWeek = account.FirstDayOfWeek.ToGrpc()
        };

    private async Task<ManagementAccountReply> GetAccountAsync(AccountId id, CancellationToken ct) =>
        ToGrpc(await _mediator.Send(new GetAccountByIdQuery(id), ct));

    private static ManagementAccountReply.Types.Connection ToGrpc(Connection connection) =>
        connection switch
        {
            EmailConnection x =>
                new ManagementAccountReply.Types.Connection
                {
                    Type = x.Type.ToGrpc(),
                    Value = x.Value,
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                    Email = new People.Grpc.Gateway.EmailConnection
                    {
                        IsPrimary = x.IsPrimary
                    }
                },

            GoogleConnection x =>
                new ManagementAccountReply.Types.Connection
                {
                    Type = x.Type.ToGrpc(),
                    Value = x.Value,
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                    Social = new SocialConnection
                    {
                        FirstName = x.FirstName,
                        LastName = x.LastName
                    }
                },

            MicrosoftConnection x =>
                new ManagementAccountReply.Types.Connection
                {
                    Type = x.Type.ToGrpc(),
                    Value = x.Value,
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                    Social = new SocialConnection
                    {
                        FirstName = x.FirstName,
                        LastName = x.LastName
                    }
                },

            _ => throw new ArgumentOutOfRangeException(nameof(connection))
        };
}
