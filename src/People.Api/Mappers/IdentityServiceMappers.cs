using Google.Protobuf.WellKnownTypes;
using People.Api.Application.Models;
using People.Domain.AggregateModels.Account;

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
                Profile = account.Profile.ToProfile(),
                Timezone = account.Timezone.ToTimezone(),
                UpdatedAt = account.UpdatedAt.ToTimestamp()
            };

        public static People.Grpc.Identity.SignUpReply ToSignUpReply(this SignUpResult result) =>
            new()
            {
                Id = result.Id.ToAccountId(),
                DisplayName = result.FullName,
                IsSentConfirmation = result.IsSentConfirmation
            };
        
        
        public static People.Grpc.Identity.SignInReply ToSignInReply(this SignInResult result) =>
            new()
            {
                Id = result.Id.ToAccountId(),
                DisplayName = result.FullName
            };
    }
}