using System;
using Google.Protobuf.WellKnownTypes;
using People.Account.Domain.Aggregates.AccountAggregate;
using People.Account.Domain.Aggregates.AccountAggregate.Identities;
using Identity = People.Account.Domain.Aggregates.AccountAggregate.Identities.Identity;

namespace People.Account.Api.Mappers
{
    public static class CommonMapper
    {
        public static People.Grpc.Common.AccountId ToGrpc(this AccountId id) =>
            new()
            {
                Value = (long)id
            };

        public static AccountId FromGrpc(this People.Grpc.Common.AccountId id) =>
            new(id.Value);

        public static People.Grpc.Common.Identity ToIdentityKey(this Identity identity) =>
            new()
            {
                Type = identity.Type.ToIdentityType(),
                Value = identity.Value
            };

        public static Identity FromGrpc(this People.Grpc.Common.Identity identity) =>
            identity.Type switch
            {
                People.Grpc.Common.IdentityType.Email => new Identity.Email(identity.Value),
                People.Grpc.Common.IdentityType.Google => new Identity.Google(identity.Value),
                People.Grpc.Common.IdentityType.Microsoft => new Identity.Microsoft(identity.Value),
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

        public static People.Grpc.Common.PrimaryEmail ToPrimaryEmail(this EmailConnection email) =>
            new()
            {
                Email = email.Value,
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
                FullName = name.FullName(),
                PreferNickname = name.PreferNickname
            };

        public static People.Grpc.Common.DayOfWeek ToDayOfWeek(this DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                DayOfWeek.Sunday => People.Grpc.Common.DayOfWeek.Sunday,
                DayOfWeek.Monday => People.Grpc.Common.DayOfWeek.Monday,
                DayOfWeek.Tuesday => People.Grpc.Common.DayOfWeek.Tuesday,
                DayOfWeek.Wednesday => People.Grpc.Common.DayOfWeek.Wednesday,
                DayOfWeek.Thursday => People.Grpc.Common.DayOfWeek.Thursday,
                DayOfWeek.Friday => People.Grpc.Common.DayOfWeek.Friday,
                DayOfWeek.Saturday => People.Grpc.Common.DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        
        public static DayOfWeek ToDayOfWeek(this People.Grpc.Common.DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                People.Grpc.Common.DayOfWeek.Sunday => DayOfWeek.Sunday,
                People.Grpc.Common.DayOfWeek.Monday => DayOfWeek.Monday,
                People.Grpc.Common.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
                People.Grpc.Common.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
                People.Grpc.Common.DayOfWeek.Thursday => DayOfWeek.Thursday,
                People.Grpc.Common.DayOfWeek.Friday => DayOfWeek.Friday,
                People.Grpc.Common.DayOfWeek.Saturday => DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
    }
}
