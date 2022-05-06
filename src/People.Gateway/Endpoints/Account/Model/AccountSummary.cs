using System;

namespace People.Gateway.Endpoints.Account.Model;

internal sealed record AccountSummary(
    long Id,
    string Nickname,
    string? FirstName,
    string? LastName,
    string FullName,
    string Language,
    string Picture,
    string? CountryCode,
    string TimeZone,
    DayOfWeek WeekStart,
    bool IsBanned
);
