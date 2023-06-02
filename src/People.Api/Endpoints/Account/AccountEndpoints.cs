using System.Net.Mail;
using System.Security.Claims;
using MediatR;
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

        group.MapGet("/{id:long}", async (long id, IMediator mediator, CancellationToken ct) =>
            {
                var query = new GetAccountSummaryQuery(id);
                var result = await mediator.Send(query, ct);

                return result.ToModel();
            })
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        group.MapGet("/me", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                var query = new GetAccountDetailsQuery(user.GetAccountId());
                var result = await mediator.Send(query, ct);

                return result.ToModel();
            })
            .RequireAuthorization(Policy.RequireAuthenticatedUser.Name);

        group.MapPut("/me", async (UpdateRequest request, ClaimsPrincipal user, IMediator mediator,
                CancellationToken ct) =>
            {
                var command = request.ToCommand(user.GetAccountId());
                var result = await mediator.Send(command, ct);

                return result.ToModel();
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<UpdateRequest>>();

        group.MapPost("/me/emails", async (EmailRequest request, ClaimsPrincipal user, IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new AppendEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
                var result = await mediator.Send(command, ct);

                return result.ToModel();
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapDelete("/me/emails/{email}", async (string email, ClaimsPrincipal user, IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new DeleteEmailCommand(user.GetAccountId(), new MailAddress(email));
                await mediator.Send(command, ct);

                return TypedResults.Empty;
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        group.MapPost("/me/emails/status", async (EmailRequest request, ClaimsPrincipal user, IMediator mediator,
                CancellationToken ct) =>
            {
                var id = user.GetAccountId();
                var command = new ChangePrimaryEmailCommand(id, new MailAddress(request.Email));
                await mediator.Send(command, ct);

                var query = new GetEmailsQuery(id);
                var emails = await mediator.Send(query, ct);

                return emails.Select(x => x.ToModel());
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPost("/me/emails/verify", async (EmailRequest request, ClaimsPrincipal user, IMediator mediator,
                CancellationToken ct) =>
            {
                var command = new ConfirmingEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
                var token = await mediator.Send(command, ct);

                return new { token };
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<EmailRequest>>();

        group.MapPut("/me/emails/verify", async (VerifyRequest request, IMediator mediator, CancellationToken ct) =>
            {
                var command = new ConfirmEmailCommand(request.Token, request.Code);
                var result = await mediator.Send(command, ct);

                return result.ToModel();
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddEndpointFilter<ValidatorFilter<VerifyRequest>>();

        group.MapDelete("/me/connections/google/identities/{id}", async (string id, ClaimsPrincipal user,
                IMediator mediator, CancellationToken ct) =>
            {
                var command = new DeleteGoogleCommand(user.GetAccountId(), id);
                await mediator.Send(command, ct);

                return TypedResults.Empty;
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        group.MapDelete("/me/connections/microsoft/identities/{id}", async (string id, ClaimsPrincipal user,
                IMediator mediator, CancellationToken ct) =>
            {
                var command = new DeleteMicrosoftCommand(user.GetAccountId(), id);
                await mediator.Send(command, ct);

                return TypedResults.Empty;
            })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        return routes;
    }
}
