using MongoDB.Bson;
using People.Api.Mappers;
using People.Grpc.Common;
using People.Grpc.Identity;
using AccountId = People.Domain.Aggregates.AccountAggregate.AccountId;

namespace People.Api.Grpc;

internal sealed partial class IdentityService
{
    private static SignInReply ToSignInReply(AccountId id, string fullName) =>
        new()
        {
            Id = id.ToGrpc(),
            DisplayName = fullName
        };

    private static SignUpReply ToSignUpReply(AccountId id, string fullName) =>
        new()
        {
            Id = id.ToGrpc(),
            DisplayName = fullName
        };

    private static SignUpReply ToSignUpReply(AccountId id, string fullName, ObjectId confirmationId) =>
        new()
        {
            Id = id.ToGrpc(),
            DisplayName = fullName,
            Confirmation = new Confirming
            {
                Id = confirmationId.ToString()
            }
        };

    private static AccountReply ToAccountReply(Domain.Aggregates.AccountAggregate.Account account) =>
        new()
        {
            Id = account.Id.ToGrpc(),
            CountryCode = account.CountryCode.IsEmpty() ? null : account.CountryCode.ToString(),
            Ban = account.Ban.ToGrpc(),
            Name = account.Name.ToGrpc(),
            Email = account.GetPrimaryEmail().ToGrpc(),
            Language = account.Language.ToString(),
            Picture = account.Picture.ToString(),
            TimeZone = account.TimeZone,
            FirstDayOfWeek = account.FirstDayOfWeek.ToGrpc(),
            Roles = { account.Roles }
        };
}
