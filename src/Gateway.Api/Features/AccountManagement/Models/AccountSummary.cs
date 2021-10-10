using System;

namespace Gateway.Api.Features.AccountManagement.Models;

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
