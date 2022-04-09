using System;
using System.Linq;
using People.Gateway.Mappes;
using People.Gateway.Features.Account.Models;
using People.Grpc.Gateway;

namespace People.Gateway.Features.Account;

public sealed partial class ManagementController
{
    private static ManagementAccount ToAccount(ManagementAccountReply account) =>
        new(
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
            account.Ban is null ? null : new Ban(account.Ban.Reason, account.Ban.ExpiresAt?.ToDateTime()),
            account.IsPasswordAvailable,
            account.CreatedAt.ToDateTime(),
            account.LastSignIn.ToDateTime(),
            account.Roles,
            account.Connections.Select(ToGrpc)
        );


    private static Models.Connection ToGrpc(ManagementAccountReply.Types.Connection connection) =>
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
