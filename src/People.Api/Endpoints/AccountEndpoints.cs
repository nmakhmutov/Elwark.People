using System.Net.Mail;
using System.Security.Claims;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Filters;
using People.Application.Commands.AppendEmail;
using People.Application.Commands.ChangePrimaryEmail;
using People.Application.Commands.ConfirmEmail;
using People.Application.Commands.ConfirmingEmail;
using People.Application.Commands.DeleteAccount;
using People.Application.Commands.DeleteEmail;
using People.Application.Commands.DeleteGoogle;
using People.Application.Commands.DeleteMicrosoft;
using People.Application.Commands.UpdateAccount;
using People.Application.Queries.GetAccountDetails;
using People.Application.Queries.GetAccountSummary;
using People.Application.Queries.GetEmails;
using People.Domain.Entities;
using People.Domain.ValueObjects;

namespace People.Api.Endpoints;

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/accounts")
            .WithTags("Accounts");

        group.MapGet("/{id:long}", GetAccountByIdAsync)
            .RequireAuthorization(Policy.RequireRead.Name);

        group.MapGet("/me", GetMyAccountAsync)
            .RequireAuthorization(Policy.RequireWrite.Name);

        group.MapPut("/me", UpdateAccountAsync)
            .RequireAuthorization(Policy.RequireWrite.Name)
            .AddEndpointFilter<ValidatorFilter<UpdateRequest>>();

        group.MapDelete("/me", DeleteAccountAsync)
            .RequireAuthorization(Policy.RequireWrite.Name);

        group.MapPost("/me/emails", AppendEmailAsync)
            .RequireAuthorization(Policy.RequireWrite.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapDelete("/me/emails/{email}", DeleteEmailAsync)
            .RequireAuthorization(Policy.RequireWrite.Name);

        group.MapPost("/me/emails/status", ChangePrimaryEmailAsync)
            .RequireAuthorization(Policy.RequireWrite.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPost("/me/emails/verify", ConfirmingEmailAsync)
            .RequireAuthorization(Policy.RequireWrite.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPut("/me/emails/verify", ConfirmEmailAsync)
            .RequireAuthorization(Policy.RequireWrite.Name)
            .AddEndpointFilter<ValidatorFilter<VerifyRequest>>();

        group.MapDelete("/me/connections/google/identities/{id}", DeleteGoogleIdentityAsync)
            .RequireAuthorization(Policy.RequireWrite.Name);

        group.MapDelete("/me/connections/microsoft/identities/{id}", DeleteMicrosoftIdentityAsync)
            .RequireAuthorization(Policy.RequireWrite.Name);

        return routes;
    }

    private static async Task<AccountSummaryResponse> GetAccountByIdAsync(long id, ISender sender, CancellationToken ct)
    {
        var query = new GetAccountSummaryQuery(id);
        var result = await sender.Send(query, ct);

        return AccountSummaryResponse.Map(result);
    }

    private static async Task<AccountDetailsResponse> GetMyAccountAsync(
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var query = new GetAccountDetailsQuery(principal.GetAccountId());
        var result = await sender.Send(query, ct);

        return AccountDetailsResponse.Map(result);
    }

    private static async Task<AccountDetailsResponse> UpdateAccountAsync(
        UpdateRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = request.ToCommand(principal.GetAccountId());
        var result = await sender.Send(command, ct);

        return AccountDetailsResponse.Map(result);
    }

    private static async Task<EmptyHttpResult> DeleteAccountAsync(
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new DeleteAccountCommand(principal.GetAccountId());
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    private static async Task<EmailResponse> AppendEmailAsync(
        EmailRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new AppendEmailCommand(principal.GetAccountId(), new MailAddress(request.Email));
        var result = await sender.Send(command, ct);

        return EmailResponse.Map(result);
    }

    private static async Task<EmptyHttpResult> DeleteEmailAsync(
        string email,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new DeleteEmailCommand(principal.GetAccountId(), new MailAddress(email));
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    private static async Task<IEnumerable<EmailResponse>> ChangePrimaryEmailAsync(
        EmailRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var id = principal.GetAccountId();
        var command = new ChangePrimaryEmailCommand(id, new MailAddress(request.Email));
        await sender.Send(command, ct);

        var query = new GetEmailsQuery(id);
        var emails = await sender.Send(query, ct);

        return emails.Select(x => EmailResponse.Map(x));
    }

    private static async Task<ConfirmingTokenModel> ConfirmingEmailAsync(
        EmailRequest request,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new ConfirmingEmailCommand(principal.GetAccountId(), new MailAddress(request.Email));
        var token = await sender.Send(command, ct);

        return token;
    }

    private static async Task<EmailResponse> ConfirmEmailAsync(
        VerifyRequest request,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new ConfirmEmailCommand(request.Token, request.Code);
        var result = await sender.Send(command, ct);

        return EmailResponse.Map(result);
    }

    private static async Task<EmptyHttpResult> DeleteGoogleIdentityAsync(
        string id,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new DeleteGoogleCommand(principal.GetAccountId(), id);
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    private static async Task<EmptyHttpResult> DeleteMicrosoftIdentityAsync(
        string id,
        ClaimsPrincipal principal,
        ISender sender,
        CancellationToken ct
    )
    {
        var command = new DeleteMicrosoftCommand(principal.GetAccountId(), id);
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    internal sealed record AccountSummaryResponse(
        long Id,
        string Email,
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
        internal static AccountSummaryResponse Map(AccountSummary result) =>
            new(
                result.Id,
                result.Email,
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

    internal sealed record AccountDetailsResponse(
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
        IEnumerable<EmailResponse> Emails,
        IEnumerable<ConnectionResponse> Connections
    )
    {
        internal static AccountDetailsResponse Map(Account account) =>
            new(
                account.Id,
                account.Name.Nickname,
                account.Name.PreferNickname,
                account.Name.FirstName,
                account.Name.LastName,
                account.Name.FullName(),
                account.Language.ToString(),
                account.Picture,
                account.Region.IsEmpty() ? null : account.Region.ToString(),
                account.Country.IsEmpty() ? null : account.Country.ToString(),
                account.TimeZone.ToString(),
                account.DateFormat.ToString(),
                account.TimeFormat.ToString(),
                account.StartOfWeek,
                account.GetCreatedDateTime(),
                account.Emails.Select(x => EmailResponse.Map(x)),
                account.Externals.Select(x => ConnectionResponse.Map(x))
            );
    }

    internal sealed record ConnectionResponse(
        ExternalService Type,
        string Identity,
        string? FirstName,
        string? LastName
    )
    {
        internal static ConnectionResponse Map(ExternalConnection x) =>
            new(x.Type, x.Identity, x.FirstName, x.LastName);
    }

    internal sealed record EmailResponse(string Value, bool IsPrimary, bool IsConfirmed)
    {
        internal static EmailResponse Map(EmailAccount x) =>
            new(x.Email, x.IsPrimary, x.IsConfirmed);

        internal static EmailResponse Map(UserEmail x) =>
            new(x.Email, x.IsPrimary, x.IsConfirmed);
    }

    internal sealed record EmailRequest(string Email)
    {
        internal sealed class Validator : AbstractValidator<EmailRequest>
        {
            public Validator() =>
                RuleFor(x => x.Email)
                    .NotEmpty()
                    .EmailAddress();
        }
    }

    internal sealed record UpdateRequest(
        string? FirstName,
        string? LastName,
        string Nickname,
        bool PreferNickname,
        string Language,
        string CountryCode,
        string TimeZone,
        string DateFormat,
        string TimeFormat,
        DayOfWeek StartOfWeek
    )
    {
        public UpdateAccountCommand ToCommand(AccountId id) =>
            new(
                id,
                FirstName,
                LastName,
                Nickname,
                PreferNickname,
                Domain.ValueObjects.Language.Parse(Language),
                Domain.ValueObjects.TimeZone.Parse(TimeZone),
                Domain.ValueObjects.DateFormat.Parse(DateFormat),
                Domain.ValueObjects.TimeFormat.Parse(TimeFormat),
                StartOfWeek,
                Domain.ValueObjects.CountryCode.Parse(CountryCode)
            );

        internal sealed class Validator : AbstractValidator<UpdateRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Nickname)
                    .NotEmpty()
                    .MinimumLength(3)
                    .MaximumLength(Name.NicknameLength);

                RuleFor(x => x.FirstName)
                    .MaximumLength(Name.FirstNameLength);

                RuleFor(x => x.LastName)
                    .MaximumLength(Name.LastNameLength);

                RuleFor(x => x.Language)
                    .NotEmpty()
                    .Length(Domain.ValueObjects.Language.MaxLength)
                    .Must(x => Domain.ValueObjects.Language.TryParse(x, out _));

                RuleFor(x => x.CountryCode)
                    .NotEmpty()
                    .Length(Domain.ValueObjects.CountryCode.MaxLength)
                    .Must(x => Domain.ValueObjects.CountryCode.TryParse(x, out _));

                RuleFor(x => x.TimeZone)
                    .NotEmpty()
                    .MaximumLength(Domain.ValueObjects.TimeZone.MaxLength)
                    .Must(x => Domain.ValueObjects.TimeZone.TryParse(x, out _));

                RuleFor(x => x.DateFormat)
                    .NotEmpty()
                    .MaximumLength(Domain.ValueObjects.DateFormat.MaxLength)
                    .Must(x => Domain.ValueObjects.DateFormat.TryParse(x, out _));

                RuleFor(x => x.TimeFormat)
                    .NotEmpty()
                    .MaximumLength(Domain.ValueObjects.TimeFormat.MaxLength)
                    .Must(x => Domain.ValueObjects.TimeFormat.TryParse(x, out _));

                RuleFor(x => x.StartOfWeek)
                    .IsInEnum();
            }
        }
    }

    internal sealed record VerifyRequest(string Token, string Code)
    {
        internal sealed class Validator : AbstractValidator<VerifyRequest>
        {
            public Validator()
            {
                RuleFor(x => x.Token)
                    .NotEmpty();

                RuleFor(x => x.Code)
                    .NotEmpty();
            }
        }
    }
}
