using System;
using System.Collections.Generic;

namespace People.Gateway.Endpoints.Account.Model;

internal sealed record AccountDetails(
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
    string DateFormat,
    string TimeFormat,
    DayOfWeek WeekStart,
    Ban? Ban,
    bool IsPasswordAvailable,
    DateTime CreatedAt,
    IEnumerable<Connection> Connections
);
