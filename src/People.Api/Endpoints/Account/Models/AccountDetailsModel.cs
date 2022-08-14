namespace People.Api.Endpoints.Account.Models;

internal sealed record AccountDetailsModel(
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
    DayOfWeek StartOfWeek,
    DateTime CreatedAt,
    IEnumerable<EmailModel> Emails,
    IEnumerable<ConnectionModel> Connections
);