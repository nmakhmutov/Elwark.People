using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Models;
using People.Api.Application.Queries.GetAccountSummary;
using People.Domain.ValueObjects;
using People.Grpc.People;

namespace People.Api.Grpc;

internal static class PeopleGrpcMapper
{
    internal static AccountReply ToGrpc(this AccountSummary account) =>
        new()
        {
            Id = account.Id,
            Nickname = account.Name.Nickname,
            FirstName = account.Name.FirstName,
            LastName = account.Name.LastName,
            Picture = account.Picture,
            CountryCode = account.CountryCode.ToString(),
            TimeZone = account.TimeZone.ToString(),
            Language = account.Language.ToString(),
            Ban = account.Ban.ToGrpc(),
            Roles = { account.Roles }
        };

    internal static SignInReply ToGrpc(this SignInResult result) =>
        new() { Id = result.Id, FullName = result.FullName };

    internal static SignUpReply ToGrpc(this SignUpResult result) =>
        new() { Id = result.Id, FullName = result.FullName };
    
    private static AccountReply.Types.Ban? ToGrpc(this Ban? ban) =>
        ban is null ? null : new AccountReply.Types.Ban { Reason = ban.Reason, ExpiresAt = ban.ExpiredAt.ToTimestamp() };
}
