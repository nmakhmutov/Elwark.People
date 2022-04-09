using System;
using System.Linq;
using Gateway.Api.Features.Account.Models;
using Gateway.Api.Mappes;
using People.Grpc.Gateway;
using Connection = Gateway.Api.Features.Account.Models.Connection;
using EmailConnection = Gateway.Api.Features.Account.Models.EmailConnection;
using SocialConnection = Gateway.Api.Features.Account.Models.SocialConnection;

namespace Gateway.Api.Features.Account;

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


    private static Connection ToGrpc(ManagementAccountReply.Types.Connection connection) =>
        connection.ConnectionTypeCase switch
        {
            ManagementAccountReply.Types.Connection.ConnectionTypeOneofCase.Email =>
                new EmailConnection(
                    connection.Type,
                    connection.Value,
                    connection.CreatedAt.ToDateTime(),
                    connection.ConfirmedAt?.ToDateTime(),
                    connection.Email.IsPrimary
                ),

            ManagementAccountReply.Types.Connection.ConnectionTypeOneofCase.Social =>
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