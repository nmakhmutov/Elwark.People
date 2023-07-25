using People.Domain.Entities;

namespace People.Api.Endpoints.Account.Models;

internal sealed record AccountSummaryModel(
    AccountId Id,
    string Nickname,
    string? FirstName,
    string? LastName,
    string FullName,
    string Language,
    string Picture,
    string? CountryCode,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek StartOfWeek,
    bool IsBanned
);
