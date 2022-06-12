using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Endpoints.Account.Models;

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
    DateTime CreatedAt,
    IEnumerable<Email> Emails,
    IEnumerable<Connection> Connections
);

internal sealed record Email(string Value, bool IsPrimary, bool IsConfirmed);
    
internal sealed record Connection(ExternalService Type, string Identity, string? FirstName, string? LastName);
