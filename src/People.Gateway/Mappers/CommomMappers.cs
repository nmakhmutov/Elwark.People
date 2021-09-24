using System;
using System.Linq;
using People.Gateway.Features.Profile;
using People.Gateway.Models;
using People.Grpc.Gateway;
using Connection = People.Gateway.Features.Profile.Connection;
using EmailConnection = People.Gateway.Features.Profile.EmailConnection;
using SocialConnection = People.Gateway.Features.Profile.SocialConnection;

namespace People.Gateway.Mappers
{
    internal static class CommonMappers
    {
        
        public static Profile ToProfile(this ProfileReply profile) =>
            new(
                profile.Id.Value,
                profile.Name.Nickname,
                profile.Name.PreferNickname,
                profile.Name.FirstName,
                profile.Name.LastName,
                profile.Name.FullName,
                profile.Language,
                profile.Gender,
                profile.DateOfBirth?.ToDateTime(),
                profile.Bio,
                profile.Picture,
                profile.Address.ToAddress(),
                profile.TimeZone,
                profile.FirstDayOfWeek.ToDayOfWeek(),
                profile.Ban.ToBan(),
                profile.IsPasswordAvailable,
                profile.CreatedAt.ToDateTime(),
                profile.Connections.Select(x => (Connection) (x.ConnectionTypeCase switch
                {
                    Grpc.Gateway.Connection.ConnectionTypeOneofCase.Email =>
                        new EmailConnection(x.Type, x.Value, x.IsConfirmed, x.Email.IsPrimary),

                    Grpc.Gateway.Connection.ConnectionTypeOneofCase.Social =>
                        new SocialConnection(x.Type, x.Value, x.IsConfirmed, x.Social.FirstName, x.Social.LastName),
                    
                    _ => throw new ArgumentOutOfRangeException()
                }))
            );

        public static Address ToAddress(this Grpc.Common.Address address) =>
            new(
                string.IsNullOrEmpty(address.CountryCode) ? null : address.CountryCode,
                string.IsNullOrEmpty(address.CityName) ? null : address.CityName
            );

        public static Ban? ToBan(this Grpc.Common.Ban? ban) =>
            ban is null ? null : new Ban(ban.Reason, ban.ExpiresAt?.ToDateTime());
        
        public static People.Grpc.Common.DayOfWeek ToDayOfWeek(this DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                DayOfWeek.Sunday => Grpc.Common.DayOfWeek.Sunday,
                DayOfWeek.Monday => Grpc.Common.DayOfWeek.Monday,
                DayOfWeek.Tuesday => Grpc.Common.DayOfWeek.Tuesday,
                DayOfWeek.Wednesday => Grpc.Common.DayOfWeek.Wednesday,
                DayOfWeek.Thursday => Grpc.Common.DayOfWeek.Thursday,
                DayOfWeek.Friday => Grpc.Common.DayOfWeek.Friday,
                DayOfWeek.Saturday => Grpc.Common.DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        
        public static DayOfWeek ToDayOfWeek(this People.Grpc.Common.DayOfWeek dayOfWeek) =>
            dayOfWeek switch
            {
                Grpc.Common.DayOfWeek.Sunday => DayOfWeek.Sunday,
                Grpc.Common.DayOfWeek.Monday => DayOfWeek.Monday,
                Grpc.Common.DayOfWeek.Tuesday => DayOfWeek.Tuesday,
                Grpc.Common.DayOfWeek.Wednesday => DayOfWeek.Wednesday,
                Grpc.Common.DayOfWeek.Thursday => DayOfWeek.Thursday,
                Grpc.Common.DayOfWeek.Friday => DayOfWeek.Friday,
                Grpc.Common.DayOfWeek.Saturday => DayOfWeek.Saturday,
                _ => throw new ArgumentOutOfRangeException(nameof(dayOfWeek), dayOfWeek, null)
            };
        
    }
}
