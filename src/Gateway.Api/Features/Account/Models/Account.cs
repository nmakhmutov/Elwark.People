using System;

namespace Gateway.Api.Features.Account.Models;

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