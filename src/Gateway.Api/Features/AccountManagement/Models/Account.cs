using System;
using System.Collections.Generic;

namespace Gateway.Api.Features.AccountManagement.Models;

internal sealed record Account(
    long Id,
    string Nickname,
    bool PreferNickname,
    string? FirstName,
    string? LastName,
    string FullName,
    string Language,
    string Picture,
    string CountryCode,
    string TimeZone,
    DayOfWeek FirstDayOfWeek,
    Ban? Ban,
    bool IsPasswordAvailable,
    DateTime CreatedAt,
    DateTime LastSignIn,
    IEnumerable<string> Roles,
    IEnumerable<Connection> Connections
);
