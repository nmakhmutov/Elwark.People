using System;

namespace Gateway.Api.Features.Management.Models;

internal sealed record AccountModel(
    long Id,
    string FirstName,
    string LastName,
    string Nickname,
    string Picture,
    string CountryCode,
    string TimeZone,
    DateTime CreatedAt
);
