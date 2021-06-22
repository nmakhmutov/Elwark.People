using System;
using Google.Protobuf.WellKnownTypes;
using People.Domain.Aggregates.Account;
using People.Domain.Aggregates.Account.Identities;
using Identity = People.Domain.Aggregates.Account.Identities.Identity;
using IdentityType = People.Domain.Aggregates.Account.Identities.IdentityType;

namespace People.Api.Mappers
{
    public static class CommonMapper
    {
        public static People.Grpc.Common.AccountId ToAccountId(this AccountId id) =>
            new()
            {
                Value = (long) id
            };

        public static AccountId ToAccountId(this People.Grpc.Common.AccountId id) =>
            new(id.Value);

        public static People.Grpc.Common.Identity ToIdentityKey(this Identity identity) =>
            new()
            {
                Type = identity.Type.ToIdentityType(),
                Value = identity.Value
            };

        public static Identity ToIdentityKey(this People.Grpc.Common.Identity identity) =>
            identity.Type switch
            {
                People.Grpc.Common.IdentityType.Email => new EmailIdentity(identity.Value),
                People.Grpc.Common.IdentityType.Google => new GoogleIdentity(identity.Value),
                People.Grpc.Common.IdentityType.Microsoft => new MicrosoftIdentity(identity.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "Unknown identity")
            };

        public static IdentityType ToIdentityType(this People.Grpc.Common.IdentityType type) =>
            type switch
            {
                People.Grpc.Common.IdentityType.Email => IdentityType.Email,
                People.Grpc.Common.IdentityType.Google => IdentityType.Google,
                People.Grpc.Common.IdentityType.Microsoft => IdentityType.Microsoft,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public static People.Grpc.Common.EmailType ToEmailType(this EmailType type) =>
            type switch
            {
                EmailType.None => People.Grpc.Common.EmailType.None,
                EmailType.Primary => People.Grpc.Common.EmailType.PrimaryEmail,
                EmailType.Secondary => People.Grpc.Common.EmailType.SecondaryEmail,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public static EmailType ToEmailType(this People.Grpc.Common.EmailType type) =>
            type switch
            {
                People.Grpc.Common.EmailType.None => EmailType.None,
                People.Grpc.Common.EmailType.PrimaryEmail => EmailType.Primary,
                People.Grpc.Common.EmailType.SecondaryEmail => EmailType.Secondary,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public static People.Grpc.Common.IdentityType ToIdentityType(this IdentityType type) =>
            type switch
            {
                IdentityType.Email => People.Grpc.Common.IdentityType.Email,
                IdentityType.Google => People.Grpc.Common.IdentityType.Google,
                IdentityType.Microsoft => People.Grpc.Common.IdentityType.Microsoft,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

        public static People.Grpc.Common.Gender ToGender(this Gender gender) =>
            gender switch
            {
                Gender.Female => People.Grpc.Common.Gender.Female,
                Gender.Male => People.Grpc.Common.Gender.Male,
                _ => throw new ArgumentOutOfRangeException(nameof(Gender), gender, "Unknown gender")
            };

        public static Gender FromGrpc(this People.Grpc.Common.Gender gender) =>
            gender switch
            {
                People.Grpc.Common.Gender.Female => Gender.Female,
                People.Grpc.Common.Gender.Male => Gender.Male,
                _ => throw new ArgumentOutOfRangeException(nameof(gender), gender, "Unknown gender")
            };

        public static People.Grpc.Common.Address ToAddress(this Address address) =>
            new()
            {
                CityName = address.City,
                CountryCode = address.CountryCode.IsEmpty()
                    ? string.Empty
                    : address.CountryCode.ToString()
            };

        public static People.Grpc.Common.PrimaryEmail ToPrimaryEmail(this AccountEmail email) =>
            new()
            {
                Email = email.Address,
                IsConfirmed = email.IsConfirmed
            };

        public static People.Grpc.Common.Ban? ToBan(this Ban? ban) =>
            ban switch
            {
                PermanentBan x => new People.Grpc.Common.Ban
                {
                    Reason = x.Reason,
                    ExpiresAt = null
                },

                TemporaryBan x => new People.Grpc.Common.Ban
                {
                    Reason = x.Reason,
                    ExpiresAt = x.ExpiredAt.ToTimestamp()
                },

                null => null,

                _ => throw new ArgumentOutOfRangeException(nameof(ban), ban, "Unknown ban type")
            };

        public static People.Grpc.Common.Name ToName(this Name name) =>
            new()
            {
                Nickname = name.Nickname,
                FirstName = name.FirstName,
                LastName = name.LastName,
                FullName = name.FullName()
            };

        public static People.Grpc.Common.Timezone ToTimezone(this Timezone timezone) =>
            new()
            {
                Name = timezone.Name,
                Offset = timezone.Offset.ToDuration()
            };
    }
}
