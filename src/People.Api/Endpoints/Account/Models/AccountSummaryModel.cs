namespace People.Api.Endpoints.Account.Models;

internal sealed record AccountSummaryModel(
    long Id,
    string Nickname,
    string? FirstName,
    string? LastName,
    string FullName,
    string Language,
    string Picture,
    string? ContinentCode,
    string? CountryCode,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek StartOfWeek,
    bool IsBanned
);
