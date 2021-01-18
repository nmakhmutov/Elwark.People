using System;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using Identity = People.Domain.AggregateModels.Account.Identities.Identity;
using IdentityType = People.Domain.AggregateModels.Account.Identities.IdentityType;

namespace People.Api.Mappers
{
    public static class CommonMapper
    {
        public static People.Grpc.Common.AccountId ToGrpcAccountId(this AccountId id) =>
            new()
            {
                Value = (long) id
            };

        public static AccountId FromGrpcAccountId(this People.Grpc.Common.AccountId id) =>
            new(id.Value);

        public static People.Grpc.Common.Identity ToGrpcIdentityKey(this Identity identity) =>
            new()
            {
                Type = identity.Type.ToGrpcIdentityType(),
                Value = identity.Value
            };

        public static Identity FromGrpcIdentityKey(this People.Grpc.Common.Identity identity) =>
            identity.Type switch
            {
                People.Grpc.Common.IdentityType.Email => new EmailIdentity(identity.Value),
                People.Grpc.Common.IdentityType.Google => new GoogleIdentity(identity.Value),
                People.Grpc.Common.IdentityType.Microsoft => new MicrosoftIdentity(identity.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "Unknown identity")
            };

        public static IdentityType FromGrpcIdentityType(this People.Grpc.Common.IdentityType type) =>
            type switch
            {
                People.Grpc.Common.IdentityType.Email => IdentityType.Email,
                People.Grpc.Common.IdentityType.Google => IdentityType.Google,
                People.Grpc.Common.IdentityType.Microsoft => IdentityType.Microsoft,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public static People.Grpc.Common.IdentityType ToGrpcIdentityType(this IdentityType type) =>
            type switch
            {
                IdentityType.Email => People.Grpc.Common.IdentityType.Email,
                IdentityType.Google => People.Grpc.Common.IdentityType.Google,
                IdentityType.Microsoft => People.Grpc.Common.IdentityType.Microsoft,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        
        public static People.Grpc.Common.Gender ToGrpcGender(this Gender gender) =>
            gender switch
            {
                Gender.Female => People.Grpc.Common.Gender.Female,
                Gender.Male => People.Grpc.Common.Gender.Male,
                _ => throw new ArgumentOutOfRangeException(nameof(Gender), gender, "Unknown gender")
            };
    }
}