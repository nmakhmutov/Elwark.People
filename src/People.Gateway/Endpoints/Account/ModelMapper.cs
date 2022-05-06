using System;
using System.Linq;
using People.Gateway.Endpoints.Account.Model;
using People.Gateway.Infrastructure;
using People.Grpc.Gateway;
using Connection = People.Gateway.Endpoints.Account.Model.Connection;
using EmailConnection = People.Gateway.Endpoints.Account.Model.EmailConnection;
using SocialConnection = People.Gateway.Endpoints.Account.Model.SocialConnection;

namespace People.Gateway.Endpoints.Account;

internal static class ModelMapper
{
    internal static AccountSummary ToSummary(this AccountReply account) =>
        new(
            account.Id.Value,
            account.Name.Nickname,
            account.Name.FirstName,
            account.Name.LastName,
            account.Name.FullName,
            account.Language,
            account.Picture,
            account.CountryCode,
            account.TimeZone,
            account.WeekStart.FromGrpc(),
            account.Ban is not null
        );

    internal static AccountDetails ToDetails(this AccountReply account) =>
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
            account.DateFormat,
            account.TimeFormat,
            account.WeekStart.FromGrpc(),
            account.Ban.FromGrpc(),
            account.IsPasswordAvailable,
            account.CreatedAt.ToDateTime(),
            account.Connections.Select(x => (Connection)(x.ConnectionTypeCase switch
            {
                AccountReply.Types.Connection.ConnectionTypeOneofCase.Email =>
                    new EmailConnection(x.Type, x.Value, x.ConfirmedAt is not null, x.Email.IsPrimary),

                AccountReply.Types.Connection.ConnectionTypeOneofCase.Social =>
                    new SocialConnection(x.Type, x.Value, x.ConfirmedAt is not null, x.Social.FirstName,
                        x.Social.LastName),

                _ => throw new ArgumentOutOfRangeException()
            }))
        );

    private static Ban? FromGrpc(this Grpc.Common.Ban? ban) =>
        ban is null ? null : new Ban(ban.Reason, ban.ExpiresAt.ToDateTime());
}
