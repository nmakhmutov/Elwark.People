using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Commands.UpdateProfile;
using People.Api.Application.Queries.GetAccountById;
using People.Api.Application.Queries.GetAccounts;
using People.Api.Mappers;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Connection = People.Domain.Aggregates.AccountAggregate.Identities.Connection;
using EmailConnection = People.Grpc.Gateway.EmailConnection;

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

    public override async Task<ManagementAccountReply> GetAccount(AccountIdValue request, ServerCallContext context)
    {
        var account = await _mediator.Send(new GetAccountByIdQuery(request), context.CancellationToken);

        return ToGrpc(account);
    }

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

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id), context.CancellationToken);

        return ToGrpc(account);
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

    private static global::People.Grpc.Gateway.ManagementAccountReply.Types.Connection ToGrpc(Connection connection) =>
        connection switch
        {
            Domain.Aggregates.AccountAggregate.Identities.EmailConnection x =>
                new ManagementAccountReply.Types.Connection
                {
                    Type = x.Type.ToGrpc(),
                    Value = x.Value,
                    CreatedAt = x.CreatedAt.ToTimestamp(),
                    ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                    Email = new EmailConnection
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
