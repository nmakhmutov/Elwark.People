using People.Api.Application.Queries.GetAccountSummary;

namespace People.Api.Endpoints.Account.Models;

internal sealed record AccountSummaryModel(
    long Id,
    string Nickname,
    string? FirstName,
    string? LastName,
    string FullName,
    string Language,
    string Picture,
    string? RegionCode,
    string? CountryCode,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek StartOfWeek,
    bool IsBanned
)
{
    internal static AccountSummaryModel Map(AccountSummary result) =>
        new(
            result.Id,
            result.Name.Nickname,
            result.Name.FirstName,
            result.Name.LastName,
            result.Name.FullName(),
            result.Language.ToString(),
            result.Picture,
            result.RegionCode.IsEmpty() ? null : result.RegionCode.ToString(),
            result.CountryCode.IsEmpty() ? null : result.CountryCode.ToString(),
            result.TimeZone.ToString(),
            result.DateFormat.ToString(),
            result.TimeFormat.ToString(),
            result.StartOfWeek,
            result.Ban is not null
        );
}
