using System;
using System.Collections.Generic;

namespace People.Gateway.Features.Profile
{
    internal sealed record Profile(
        long Id,
        string Nickname,
        bool PreferNickname,
        string? FirstName,
        string? LastName,
        string FullName,
        string Language,
        string Picture,
        string? CountryCode,
        string TimeZone,
        DayOfWeek FirstDayOfWeek,
        Ban? Ban,
        bool IsPasswordAvailable,
        DateTime CreatedAt,
        IEnumerable<Connection> Connections
    );
}
