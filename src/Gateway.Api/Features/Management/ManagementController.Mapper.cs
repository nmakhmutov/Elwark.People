using System;
using System.Linq;
using Gateway.Api.Features.Management.Models;
using Gateway.Api.Mappes;
using People.Grpc.Gateway;
using Connection = Gateway.Api.Features.Management.Models.Connection;

namespace Gateway.Api.Features.Management;

public sealed partial class ManagementController
{
    private static AccountModel ToAccount(ManagementAccountReply account) =>
        new (
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
        );


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
