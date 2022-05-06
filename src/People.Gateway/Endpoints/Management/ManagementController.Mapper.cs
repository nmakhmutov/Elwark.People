using System;
using System.Linq;
using People.Gateway.Endpoints.Management.Model;
using People.Gateway.Infrastructure;
using People.Grpc.Gateway;
using Connection = People.Gateway.Endpoints.Management.Model.Connection;
using EmailConnection = People.Gateway.Endpoints.Management.Model.EmailConnection;
using SocialConnection = People.Gateway.Endpoints.Management.Model.SocialConnection;

namespace People.Gateway.Endpoints.Management;

public sealed partial class ManagementController
{
    private static ManagementAccount ToAccount(AccountReply account) =>
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
            account.WeekStart.FromGrpc(),
            account.Ban is null ? null : new BanModel(account.Ban.Reason, account.Ban.ExpiresAt?.ToDateTime()),
            account.IsPasswordAvailable,
            account.CreatedAt.ToDateTime(),
            account.LastSignIn.ToDateTime(),
            account.Roles,
            account.Connections.Select(ToGrpc)
        );


    private static Connection ToGrpc(AccountReply.Types.Connection connection) =>
        connection.ConnectionTypeCase switch
        {
            AccountReply.Types.Connection.ConnectionTypeOneofCase.Email =>
                new EmailConnection(
                    connection.Type,
                    connection.Value,
                    connection.CreatedAt.ToDateTime(),
                    connection.ConfirmedAt?.ToDateTime(),
                    connection.Email.IsPrimary
                ),

            AccountReply.Types.Connection.ConnectionTypeOneofCase.Social =>
                new SocialConnection(
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
