using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Queries.GetAccounts;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Connections;
using People.Grpc.Common;
using People.Grpc.Gateway;
using EmailConnection = People.Domain.Aggregates.AccountAggregate.Connections.EmailConnection;
using Name = People.Grpc.Common.Name;

namespace People.Api.Mappers;

internal static class PeopleMapper
{
    internal static AccountReply ToGrpc(this Account account) =>
        new()
        {
            Id = account.Id.ToGrpc(),
            Name = account.Name.ToGrpc(),
            CountryCode = account.CountryCode.IsEmpty() ? null : account.CountryCode.ToString(),
            Language = account.Language.ToString(),
            Picture = account.Picture.ToString(),
            TimeZone = account.TimeZone.ToString(),
            DateFormat = account.DateFormat.ToString(),
            TimeFormat = account.TimeFormat.ToString(),
            WeekStart = account.WeekStart.ToGrpc(),
            Ban = account.Ban.ToGrpc(),
            IsPasswordAvailable = account.IsPasswordAvailable(),
            CreatedAt = account.CreatedAt.ToTimestamp(),
            LastSignIn = account.LastSignIn.ToTimestamp(),
            Roles = { account.Roles },
            Connections =
            {
                account.Connections.Select(x => x.ToGrpc())
            }
        };

    internal static AccountsReply.Types.Account ToGrpc(this AccountModel x) =>
        new()

        {
            Id = x.AccountId.ToGrpc(),
            Language = x.Language.ToString(),
            Name = x.Name.ToGrpc(),
            Picture = x.Picture.ToString(),
            CountryCode = x.CountryCode.ToString(),
            CreatedAt = x.CreatedAt.ToTimestamp(),
            TimeZone = x.TimeZone.ToString()
        };

    internal static Name ToGrpc(this Domain.Aggregates.AccountAggregate.Name value) =>
        new()
        {
            Nickname = value.Nickname,
            FirstName = value.FirstName,
            LastName = value.LastName,
            FullName = value.FullName(),
            PreferNickname = value.PreferNickname
        };

    internal static AccountReply.Types.Connection ToGrpc(this Connection connection) =>
        connection switch
        {
            EmailConnection x => new AccountReply.Types.Connection
            {
                Type = IdentityType.Email,
                Value = x.Value,
                ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                CreatedAt = x.CreatedAt.ToTimestamp(),
                Email = new People.Grpc.Gateway.EmailConnection { IsPrimary = x.IsPrimary }
            },

            GoogleConnection x => new AccountReply.Types.Connection
            {
                Type = IdentityType.Google,
                Value = x.Value,
                ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                CreatedAt = x.CreatedAt.ToTimestamp(),
                Social = new SocialConnection { FirstName = x.FirstName, LastName = x.LastName }
            },

            MicrosoftConnection x => new AccountReply.Types.Connection
            {
                Type = IdentityType.Microsoft,
                Value = x.Value,
                ConfirmedAt = x.ConfirmedAt?.ToTimestamp(),
                CreatedAt = x.CreatedAt.ToTimestamp(),
                Social = new SocialConnection { FirstName = x.FirstName, LastName = x.LastName }
            },

            _ => throw new ArgumentOutOfRangeException(nameof(connection))
        };
}
