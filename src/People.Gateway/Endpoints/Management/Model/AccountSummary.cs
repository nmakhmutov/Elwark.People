using System;

namespace People.Gateway.Endpoints.Management.Model;

internal sealed record AccountSummary(
    long Id,
    string FirstName,
    string LastName,
    string Nickname,
    string Picture,
    string CountryCode,
    string TimeZone,
    DateTime CreatedAt
);
