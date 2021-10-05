using System;
using System.Linq;
using Gateway.Api.Mappes;
using People.Grpc.Gateway;

namespace Gateway.Api.Features.Profile;

public sealed partial class ProfilesController
{
    private static Profile ToProfile(ProfileReply profile) =>
        new(
            profile.Id.Value,
            profile.Name.Nickname,
            profile.Name.PreferNickname,
            profile.Name.FirstName,
            profile.Name.LastName,
            profile.Name.FullName,
            profile.Language,
            profile.Picture,
            profile.CountryCode,
            profile.TimeZone,
            profile.FirstDayOfWeek.FromGrpc(),
            FromGrpc(profile.Ban),
            profile.IsPasswordAvailable,
            profile.CreatedAt.ToDateTime(),
            profile.Connections.Select(x => (Connection)(x.ConnectionTypeCase switch
            {
                People.Grpc.Gateway.Connection.ConnectionTypeOneofCase.Email => 
                    new EmailConnection(x.Type, x.Value, x.IsConfirmed, x.Email.IsPrimary),
                
                People.Grpc.Gateway.Connection.ConnectionTypeOneofCase.Social => 
                    new SocialConnection(x.Type, x.Value, x.IsConfirmed, x.Social.FirstName, x.Social.LastName),
                
                _ => throw new ArgumentOutOfRangeException()
            }))
        );

    private static Ban? FromGrpc(People.Grpc.Common.Ban? ban) =>
        ban is null ? null : new Ban(ban.Reason, ban.ExpiresAt?.ToDateTime());
}
