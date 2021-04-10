using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Grpc.Gateway;

namespace People.Api.Mappers
{
    public static class GatewayServiceMapper
    {
        public static AccountReply ToGatewayAccountReply(this Account account) =>
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
        
        public static ProfileReply ToGatewayProfileReply(this Account account) =>
            new()
            {
                Address = account.Address.ToAddress(),
                Id = account.Id.ToAccountId(),
                Name = account.Name.ToName(),
                Profile = account.Profile.ToProfile(),
                Timezone = account.Timezone.ToTimezone(),
                Ban = account.Ban.ToBan(),
                IsPasswordAvailable = account.IsPasswordAvailable(),
                CreatedAt = account.CreatedAt.ToTimestamp(),
                Identities =
                {
                    account.Identities.Select(x =>
                    {
                        var profile =  new ProfileIdentity
                        {
                            Type = x.Type.ToIdentityType(),
                            Value = x.Value,
                            IsConfirmed = x.IsConfirmed()
                        };

                        switch (x)
                        {
                            case EmailIdentityModel t:
                                profile.Email = new People.Grpc.Gateway.EmailIdentity
                                {
                                    Type = t.EmailType.ToEmailType()
                                };
                                break;
                            
                            case GoogleIdentityModel t:
                                profile.Social = new SocialIdentity
                                {
                                    Name = t.Name
                                };
                                break;
                            
                            case MicrosoftIdentityModel t:
                                profile.Social = new SocialIdentity
                                {
                                    Name = t.Name
                                };
                                break;
                            
                            default:
                                throw new ArgumentOutOfRangeException(nameof(x));
                        }

                        return profile;
                    })
                }
            };
    }
}