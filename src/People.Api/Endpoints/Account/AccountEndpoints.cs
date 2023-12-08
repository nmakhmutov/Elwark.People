using System.Net.Mail;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using People.Api.Application.Commands.AppendEmail;
using People.Api.Application.Commands.ChangePrimaryEmail;
using People.Api.Application.Commands.ConfirmEmail;
using People.Api.Application.Commands.ConfirmingEmail;
using People.Api.Application.Commands.DeleteEmail;
using People.Api.Application.Commands.DeleteGoogle;
using People.Api.Application.Commands.DeleteMicrosoft;
using People.Api.Application.Queries.GetAccountDetails;
using People.Api.Application.Queries.GetAccountSummary;
using People.Api.Application.Queries.GetEmails;
using People.Api.Endpoints.Account.Models;
using People.Api.Endpoints.Account.Requests;
using People.Api.Infrastructure;
using People.Api.Infrastructure.Filters;

namespace People.Api.Endpoints.Account;

internal static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/accounts")
            .WithTags("accounts", "me");

        group.MapGet("/{id:long}", GetAccountByIdAsync)
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        group.MapGet("/me", GetMyAccountAsync)
            .RequireAuthorization(Policy.RequireAuthenticatedUser.Name);

        group.MapPut("/me", UpdateAccountAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<UpdateRequest>>();

        group.MapPost("/me/emails", AppendEmailAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapDelete("/me/emails/{email}", DeleteEmailAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        group.MapPost("/me/emails/status", ChangePrimaryEmailAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPost("/me/emails/verify", ConfirmingEmailAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPut("/me/emails/verify", ConfirmEmailAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<VerifyRequest>>();

        group.MapDelete("/me/connections/google/identities/{id}", DeleteGoogleIdentityAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        group.MapDelete("/me/connections/microsoft/identities/{id}", DeleteMicrosoftIdentityAsync)
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        return routes;
    }

    private static async Task<AccountSummaryModel> GetAccountByIdAsync(long id, ISender sender, CancellationToken ct)
    {
        var query = new GetAccountSummaryQuery(id);
        var result = await sender.Send(query, ct);

        return AccountSummaryModel.Map(result);
    }

    private static async Task<AccountDetailsModel> GetMyAccountAsync(
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetAccountDetailsQuery(user.GetAccountId());
        var result = await sender.Send(query, ct);

        return AccountDetailsModel.Map(result);
    }

    private static async Task<AccountDetailsModel> UpdateAccountAsync(
        UpdateRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = request.ToCommand(user.GetAccountId());
        var result = await sender.Send(command, ct);

        return AccountDetailsModel.Map(result);
    }

    private static async Task<EmailModel> AppendEmailAsync(
        EmailRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = new AppendEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
        var result = await sender.Send(command, ct);

        return EmailModel.Map(result);
    }

    private static async Task<EmptyHttpResult> DeleteEmailAsync(
        string email,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteEmailCommand(user.GetAccountId(), new MailAddress(email));
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    private static async Task<IEnumerable<EmailModel>> ChangePrimaryEmailAsync(
        EmailRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var id = user.GetAccountId();
        var command = new ChangePrimaryEmailCommand(id, new MailAddress(request.Email));
        await sender.Send(command, ct);

        var query = new GetEmailsQuery(id);
        var emails = await sender.Send(query, ct);

        return emails.Select(x => EmailModel.Map(x));
    }

    private static async Task<ConfirmingTokenModel> ConfirmingEmailAsync(
        EmailRequest request,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = new ConfirmingEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
        var token = await sender.Send(command, ct);

        return token;
    }

    private static async Task<EmailModel> ConfirmEmailAsync(
        VerifyRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new ConfirmEmailCommand(request.Token, request.Code);
        var result = await sender.Send(command, ct);

        return EmailModel.Map(result);
    }

    private static async Task<EmptyHttpResult> DeleteGoogleIdentityAsync(
        string id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteGoogleCommand(user.GetAccountId(), id);
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }

    private static async Task<EmptyHttpResult> DeleteMicrosoftIdentityAsync(
        string id,
        ClaimsPrincipal user,
        ISender sender,
        CancellationToken ct)
    {
        var command = new DeleteMicrosoftCommand(user.GetAccountId(), id);
        await sender.Send(command, ct);

        return TypedResults.Empty;
    }
}
