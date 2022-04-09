using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Grpc.Gateway;
using Connection = People.Grpc.Gateway.Connection;
using EmailConnection = People.Domain.Aggregates.AccountAggregate.Identities.EmailConnection;

namespace People.Api.Mappers;

public static class AccountMapper
{
    public static ProfileReply ToProfileReply(this Account account) =>
        new()
        {
            Id = account.Id,
            Name = account.Name,
            CountryCode = account.CountryCode.IsEmpty() ? null : account.CountryCode.ToString(),
            Language = account.Language.ToString(),
            Picture = account.Picture.ToString(),
            TimeZone = account.TimeZone,
            FirstDayOfWeek = account.FirstDayOfWeek.ToGrpc(),
            Ban = account.Ban.ToGrpc(),
            IsPasswordAvailable = account.IsPasswordAvailable(),
            CreatedAt = account.CreatedAt.ToTimestamp(),
            Connections =
            {
                account.Connections.Select(ToGrpc)
            }
        };

    private static Connection ToGrpc(Domain.Aggregates.AccountAggregate.Identities.Connection connection) =>
        connection switch
        {
            EmailConnection x => new Connection
            {
                Type = x.Type.ToGrpc(), Value = x.Value, IsConfirmed = x.IsConfirmed,
                Email = new People.Grpc.Gateway.EmailConnection { IsPrimary = x.IsPrimary }
            },

            GoogleConnection x => new Connection
            {
                Type = x.Type.ToGrpc(), Value = x.Value, IsConfirmed = x.IsConfirmed,
                Social = new SocialConnection { FirstName = x.FirstName, LastName = x.LastName }
            },

            MicrosoftConnection x => new Connection
            {
                Type = x.Type.ToGrpc(), Value = x.Value, IsConfirmed = x.IsConfirmed,
                Social = new SocialConnection { FirstName = x.FirstName, LastName = x.LastName }
            },

            _ => throw new ArgumentOutOfRangeException(nameof(connection))
        };
}
