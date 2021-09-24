using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using People.Grpc.Gateway;
using Connection = People.Grpc.Gateway.Connection;
using EmailConnection = People.Account.Domain.Aggregates.AccountAggregate.Identities.EmailConnection;

namespace People.Account.Api.Mappers
{
    public static class GatewayServiceMapper
    {
        public static ProfileReply ToGatewayProfileReply(this Domain.Aggregates.AccountAggregate.Account account) =>
            new()
            {
                Address = account.Address.ToAddress(),
                Id = account.Id.ToGrpc(),
                Name = account.Name.ToName(),
                Bio = account.Bio,
                DateOfBirth = account.DateOfBirth?.ToTimestamp(),
                Gender = account.Gender.ToGender(),
                Language = account.Language.ToString(),
                Picture = account.Picture.ToString(),
                TimeZone = account.TimeZone,
                FirstDayOfWeek = account.FirstDayOfWeek.ToDayOfWeek(),
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
                                Type = x.Type.ToIdentityType(),
                                Value = x.Value,
                                IsConfirmed = x.IsConfirmed,
                                Email = new People.Grpc.Gateway.EmailConnection
                                {
                                    IsPrimary = x.IsPrimary
                                }
                            },
                        
                        GoogleConnection x =>
                            new Connection
                            {
                                Type = x.Type.ToIdentityType(),
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
                                Type = x.Type.ToIdentityType(),
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
