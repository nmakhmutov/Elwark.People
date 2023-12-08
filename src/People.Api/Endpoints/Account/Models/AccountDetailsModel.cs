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
    string? RegionCode,
    string? CountryCode,
    string TimeZone,
    string DateFormat,
    string TimeFormat,
    DayOfWeek StartOfWeek,
    DateTime CreatedAt,
    IEnumerable<EmailModel> Emails,
    IEnumerable<ConnectionModel> Connections
)
{
    internal static AccountDetailsModel Map(Domain.Entities.Account account) =>
        new(
            account.Id,
            account.Name.Nickname,
            account.Name.PreferNickname,
            account.Name.FirstName,
            account.Name.LastName,
            account.Name.FullName(),
            account.Language.ToString(),
            account.Picture,
            account.RegionCode.IsEmpty() ? null : account.RegionCode.ToString(),
            account.CountryCode.IsEmpty() ? null : account.CountryCode.ToString(),
            account.TimeZone.ToString(),
            account.DateFormat.ToString(),
            account.TimeFormat.ToString(),
            account.StartOfWeek,
            account.GetCreatedDateTime(),
            account.Emails.Select(x => EmailModel.Map(x)),
            account.Externals.Select(x => ConnectionModel.Map(x))
        );
}
