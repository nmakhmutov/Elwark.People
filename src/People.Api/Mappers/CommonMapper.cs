using System;
using Google.Protobuf.WellKnownTypes;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Grpc.Common;
using Ban = People.Grpc.Common.Ban;
using DayOfWeek = People.Grpc.Common.DayOfWeek;
using Identity = People.Grpc.Common.Identity;
using IdentityType = People.Grpc.Common.IdentityType;

namespace People.Api.Mappers;

public static class CommonMapper
{
    public static Identity ToGrpc(this Domain.Aggregates.AccountAggregate.Identities.Identity identity) =>
        new()
        {
            Type = identity.Type.ToGrpc(),
            Value = identity.Value
        };

    public static Domain.Aggregates.AccountAggregate.Identities.Identity FromGrpc(this Identity identity) =>
        identity.Type switch
        {
            IdentityType.Email =>
                new Domain.Aggregates.AccountAggregate.Identities.Identity.Email(identity.Value),
            
            IdentityType.Google =>
                new Domain.Aggregates.AccountAggregate.Identities.Identity.Google(identity.Value),
            
            IdentityType.Microsoft => 
                new Domain.Aggregates.AccountAggregate.Identities.Identity.Microsoft(identity.Value),
            
            _ => throw new ArgumentOutOfRangeException(nameof(identity), identity, "Unknown identity")
        };

    public static Domain.Aggregates.AccountAggregate.Identities.IdentityType FromGrpc(this IdentityType type) =>
        type switch
        {
            IdentityType.Email => Domain.Aggregates.AccountAggregate.Identities.IdentityType.Email,
            IdentityType.Google => Domain.Aggregates.AccountAggregate.Identities.IdentityType.Google,
            IdentityType.Microsoft => Domain.Aggregates.AccountAggregate.Identities.IdentityType.Microsoft,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static IdentityType ToGrpc(this Domain.Aggregates.AccountAggregate.Identities.IdentityType type) =>
        type switch
        {
            Domain.Aggregates.AccountAggregate.Identities.IdentityType.Email => IdentityType.Email,
            Domain.Aggregates.AccountAggregate.Identities.IdentityType.Google => IdentityType.Google,
            Domain.Aggregates.AccountAggregate.Identities.IdentityType.Microsoft => IdentityType.Microsoft,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static PrimaryEmail ToGrpc(this EmailConnection email) =>
        new()
        {
            Email = email.Value,
            IsConfirmed = email.IsConfirmed
        };

    public static Ban? ToGrpc(this Domain.Aggregates.AccountAggregate.Ban? ban) =>
        ban switch
        {
            PermanentBan x => new Ban
            {
                Reason = x.Reason,
                ExpiresAt = null
            },

            TemporaryBan x => new Ban
            {
                Reason = x.Reason,
                ExpiresAt = x.ExpiredAt.ToTimestamp()
            },

            null => null,

            _ => throw new ArgumentOutOfRangeException(nameof(ban), ban, "Unknown ban type")
        };

    public static DayOfWeek ToGrpc(this System.DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            System.DayOfWeek.Sunday => DayOfWeek.Sunday,
            System.DayOfWeek.Monday => DayOfWeek.Monday,
            System.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
            System.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
            System.DayOfWeek.Thursday => DayOfWeek.Thursday,
            System.DayOfWeek.Friday => DayOfWeek.Friday,
            System.DayOfWeek.Saturday => DayOfWeek.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };

    public static System.DayOfWeek FromGrpc(this DayOfWeek dayOfWeek) =>
        dayOfWeek switch
        {
            DayOfWeek.Sunday => System.DayOfWeek.Sunday,
            DayOfWeek.Monday => System.DayOfWeek.Monday,
            DayOfWeek.Tuesday => System.DayOfWeek.Tuesday,
            DayOfWeek.Wednesday => System.DayOfWeek.Wednesday,
            DayOfWeek.Thursday => System.DayOfWeek.Thursday,
            DayOfWeek.Friday => System.DayOfWeek.Friday,
            DayOfWeek.Saturday => System.DayOfWeek.Saturday,
            _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
        };
}
