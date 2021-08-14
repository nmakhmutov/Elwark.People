using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Models;
using People.Domain.Aggregates.Account;
using People.Grpc.Common;

namespace People.Api.Mappers
{
    public static class IdentityServiceMappers
    {
        public static People.Grpc.Identity.AccountReply ToIdentityAccountReply(this Account account) =>
            new()
            {
                Id = account.Id.ToAccountId(),
                Address = account.Address.ToAddress(),
                Ban = account.Ban.ToBan(),
                Name = account.Name.ToName(),
                Email = account.GetPrimaryEmail().ToPrimaryEmail(),
                Bio = account.Bio,
                DateOfBirth = account.DateOfBirth?.ToTimestamp(),
                Gender = account.Gender.ToGender(),
                Language = account.Language.ToString(),
                Picture = account.Picture.ToString(),
                TimeInfo = account.TimeInfo.ToTimeInfo(),
                UpdatedAt = account.UpdatedAt.ToTimestamp(),
                Roles = {account.Roles}
            };

        public static People.Grpc.Identity.SignUpReply ToSignUpReply(this SignUpResult result) =>
            new()
            {
                Id = result.Id.ToAccountId(),
                DisplayName = result.FullName,
                Confirmation = result.ConfirmationId.HasValue
                    ? new Confirming
                    {
                        Id = result.ConfirmationId.ToString()
                    }
                    : null
            };


        public static People.Grpc.Identity.SignInReply ToSignInReply(this SignInResult result) =>
            new()
            {
                Id = result.Id.ToAccountId(),
                DisplayName = result.FullName
            };
    }
}
