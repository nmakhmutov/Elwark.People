using Google.Protobuf.WellKnownTypes;
using MongoDB.Bson;
using People.Account.Api.Mappers;
using People.Grpc.Common;
using People.Grpc.Identity;
using AccountId = People.Account.Domain.Aggregates.AccountAggregate.AccountId;

namespace People.Account.Api.Grpc
{
    public sealed partial class IdentityService
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
                Address = account.Address.ToAddress(),
                Ban = account.Ban.ToBan(),
                Name = account.Name.ToName(),
                Email = account.GetPrimaryEmail().ToPrimaryEmail(),
                Bio = account.Bio,
                DateOfBirth = account.DateOfBirth?.ToTimestamp(),
                Gender = account.Gender.ToGender(),
                Language = account.Language.ToString(),
                Picture = account.Picture.ToString(),
                TimeZone = account.TimeZone,
                FirstDayOfWeek = account.FirstDayOfWeek.ToDayOfWeek(),
                UpdatedAt = account.UpdatedAt.ToTimestamp(),
                Roles = { account.Roles }
            };
    }
}
