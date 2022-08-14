using People.Api.Application.Queries.GetAccountSummary;
using People.Api.Endpoints.Account.Models;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Api.Endpoints.Account;

internal static class AccountModelMapper
{
    internal static AccountSummaryModel ToModel(this AccountSummary result) =>
        new(
            result.Id,
            result.Name.Nickname,
            result.Name.FirstName,
            result.Name.LastName,
            result.Name.FullName(),
            result.Language.ToString(),
            result.Picture,
            result.CountryCode.ToModel(),
            result.TimeZone.ToString(),
            result.DateFormat.ToString(),
            result.TimeFormat.ToString(),
            result.StartOfWeek,
            result.Ban is not null
        );

    internal static AccountDetailsModel ToModel(this Domain.AggregatesModel.AccountAggregate.Account account) =>
        new(
            account.Id,
            account.Name.Nickname,
            account.Name.PreferNickname,
            account.Name.FirstName,
            account.Name.LastName,
            account.Name.FullName(),
            account.Language.ToString(),
            account.Picture.ToString(),
            account.CountryCode.ToModel(),
            account.TimeZone.ToString(),
            account.DateFormat.ToString(),
            account.TimeFormat.ToString(),
            account.StartOfWeek,
            account.GetCreatedDateTime(),
            account.Emails.Select(x => x.ToModel()),
            account.Externals.Select(x => x.ToModel())
        );

    private static string? ToModel(this CountryCode code) =>
        code.IsEmpty() ? null : code.ToString();

    private static ConnectionModel ToModel(this ExternalConnection x) =>
        new(x.Type, x.Identity, x.FirstName, x.LastName);

    public static EmailModel ToModel(this EmailAccount x) =>
        new(x.Email, x.IsPrimary, x.IsConfirmed);

    public static EmailModel ToModel(this Application.Queries.GetEmails.Email x) =>
        new(x.Value, x.IsPrimary, x.IsConfirmed);
}
