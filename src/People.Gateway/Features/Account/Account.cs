using System;

namespace People.Gateway.Features.Account
{
    internal sealed record Account(
        long Id,
        string Nickname,
        string? FirstName,
        string? LastName,
        string FullName,
        string Language,
        string Picture,
        string? CountryCode,
        string TimeZone,
        DayOfWeek FirstDayOfWeek,
        bool IsBanned
    );
}
