using System;
using System.Collections.Generic;

namespace People.Gateway.Endpoints.Management.Model;

internal sealed record ManagementAccount(
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
    DayOfWeek WeekStart,
    BanModel? Ban,
    bool IsPasswordAvailable,
    DateTime CreatedAt,
    DateTime LastSignIn,
    IEnumerable<string> Roles,
    IEnumerable<Connection> Connections
);
