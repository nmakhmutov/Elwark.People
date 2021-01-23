using People.Domain.AggregateModels.Account;

namespace People.Api.Mappers
{
    public static class GatewayServiceMapper
    {
        public static People.Grpc.Gateway.AccountReply ToGatewayAccountReply(this Account account) =>
            new()
            {
                Address = account.Address.ToAddress(),
                Email = account.GetPrimaryEmail().ToPrimaryEmail(),
                Id = account.Id.ToAccountId(),
                Name = account.Name.ToName(),
                Profile = account.Profile.ToProfile(),
                Timezone = account.Timezone.ToTimezone(),
                IsBanned = account.Ban is not null
            };
    }
}