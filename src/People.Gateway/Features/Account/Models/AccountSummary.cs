using System;

namespace People.Gateway.Features.Account.Models;

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
