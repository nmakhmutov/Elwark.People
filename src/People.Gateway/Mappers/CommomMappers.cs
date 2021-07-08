using System;
using System.Linq;
using People.Gateway.Models;
using People.Grpc.Gateway;
using EmailIdentity = People.Gateway.Models.EmailIdentity;
using SocialIdentity = People.Gateway.Models.SocialIdentity;
using Timezone = People.Gateway.Features.Timezone.Timezone;

namespace People.Gateway.Mappers
{
    internal static class CommonMappers
    {
        
        public static Profile ToProfile(this ProfileReply profile) =>
            new(
                profile.Id.Value,
                profile.Name.Nickname,
                profile.Name.FirstName,
                profile.Name.LastName,
                profile.Name.FullName,
                profile.Language,
                profile.Gender,
                profile.DateOfBirth?.ToDateTime(),
                profile.Bio,
                profile.Picture,
                profile.Address.ToAddress(),
                profile.Timezone.ToTimezone(),
                profile.Ban.ToBan(),
                profile.IsPasswordAvailable,
                profile.CreatedAt.ToDateTime(),
                profile.Identities.Select(x => (Identity) (x.IdentityTypeCase switch
                {
                    ProfileIdentity.IdentityTypeOneofCase.Email =>
                        new EmailIdentity(x.Type, x.Value, x.IsConfirmed, x.Email.Type),

                    ProfileIdentity.IdentityTypeOneofCase.Social =>
                        new SocialIdentity(x.Type, x.Value, x.IsConfirmed, x.Social.Name),

                    _ => throw new ArgumentOutOfRangeException()
                }))
            );

        public static Address ToAddress(this Grpc.Common.Address address) =>
            new(
                string.IsNullOrEmpty(address.CountryCode) ? null : address.CountryCode,
                string.IsNullOrEmpty(address.CountryCode) ? null : address.CityName
            );

        public static Timezone ToTimezone(this Grpc.Common.Timezone timezone) =>
            new(timezone.Name, timezone.Offset.ToTimeSpan());

        public static Ban? ToBan(this Grpc.Common.Ban? ban) =>
            ban is null ? null : new Ban(ban.Reason, ban.ExpiresAt?.ToDateTime());
    }
}
