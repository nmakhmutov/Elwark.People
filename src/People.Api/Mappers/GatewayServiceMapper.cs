using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using People.Grpc.Gateway;
using Connection = People.Grpc.Gateway.Connection;
using EmailConnection = People.Domain.Aggregates.Account.Identities.EmailConnection;

namespace People.Api.Mappers
{
    public static class GatewayServiceMapper
    {
        public static ProfileReply ToGatewayProfileReply(this Account account) =>
            new()
            {
                Address = account.Address.ToAddress(),
                Id = account.Id.ToAccountId(),
                Name = account.Name.ToName(),
                Bio = account.Bio,
                DateOfBirth = account.DateOfBirth?.ToTimestamp(),
                Gender = account.Gender.ToGender(),
                Language = account.Language.ToString(),
                Picture = account.Picture.ToString(),
                TimeInfo = account.TimeInfo.ToTimeInfo(),
                Ban = account.Ban.ToBan(),
                IsPasswordAvailable = account.IsPasswordAvailable(),
                CreatedAt = account.CreatedAt.ToTimestamp(),
                Connections =
                {
                    account.Connections.Select(connection => connection switch
                    {
                        EmailConnection x =>
                            new Connection
                            {
                                Type = x.IdentityType.ToIdentityType(),
                                Value = x.Value,
                                IsConfirmed = x.IsConfirmed,
                                Email = new People.Grpc.Gateway.EmailConnection
                                {
                                    Type = x.EmailType.ToEmailType()
                                }
                            },
                        
                        GoogleConnection x =>
                            new Connection
                            {
                                Type = x.IdentityType.ToIdentityType(),
                                Value = x.Value,
                                IsConfirmed = x.IsConfirmed,
                                Social = new SocialConnection
                                {
                                    FirstName = x.FirstName,
                                    LastName = x.LastName
                                }
                            },
                        
                        MicrosoftConnection x =>
                            new Connection
                            {
                                Type = x.IdentityType.ToIdentityType(),
                                Value = x.Value,
                                IsConfirmed = x.IsConfirmed,
                                Social = new SocialConnection
                                {
                                    FirstName = x.FirstName,
                                    LastName = x.LastName
                                }
                            },

                        _ => throw new ArgumentOutOfRangeException(nameof(connection))
                    })
                }
            };
    }
}
