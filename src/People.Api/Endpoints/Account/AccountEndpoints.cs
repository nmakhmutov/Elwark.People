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
        routes.MapGet("/accounts/{id:long}", async (long id, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAccountSummaryQuery(id), ct);
                return Results.Ok(result.ToModel());
            })
            .RequireAuthorization(Policy.RequireCommonAccess.Name);

        routes.MapGet("/accounts/me", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
            {
                var result = await mediator.Send(new GetAccountDetailsQuery(user.GetAccountId()), ct);
                return Results.Ok(result.ToModel());
            })
            .RequireAuthorization(Policy.RequireAuthenticatedUser.Name);

        routes.MapPut("/accounts/me",
                async (UpdateRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    var result = await mediator.Send(request.ToCommand(user.GetAccountId()), ct);
                    return Results.Ok(result.ToModel());
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddRouteHandlerFilter<ValidatorFilter<UpdateRequest>>();

        routes.MapPost("/accounts/me/emails",
                async (EmailRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    var command = new AppendEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
                    var result = await mediator.Send(command, ct);

                    return Results.Ok(result.ToModel());
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddRouteHandlerFilter<ValidatorFilter<EmailRequest>>();

        routes.MapDelete("/accounts/me/emails/{email}",
                async (string email, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteEmailCommand(user.GetAccountId(), new MailAddress(email)), ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        routes.MapPost("/accounts/me/emails/status",
                async (EmailRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    var id = user.GetAccountId();
                    await mediator.Send(new ChangePrimaryEmailCommand(id, new MailAddress(request.Email)), ct);

                    var emails = await mediator.Send(new GetEmailsQuery(id), ct);
                    return Results.Ok(emails.Select(x => x.ToModel()));
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddRouteHandlerFilter<ValidatorFilter<EmailRequest>>();

        routes.MapPost("/accounts/me/emails/verify",
                async (EmailRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    var command = new ConfirmingEmailCommand(user.GetAccountId(), new MailAddress(request.Email));
                    return Results.Ok(new { token = await mediator.Send(command, ct) });
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddRouteHandlerFilter<ValidatorFilter<EmailRequest>>();

        routes.MapPut("/accounts/me/emails/verify",
                async (VerifyRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    var command = new ConfirmEmailCommand(request.Token, request.Code);
                    var result = await mediator.Send(command, ct);

                    return Results.Ok(result.ToModel());
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name)
            .AddRouteHandlerFilter<ValidatorFilter<VerifyRequest>>();

        routes.MapDelete("/accounts/me/connections/google/identities/{id}",
                async (string id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteGoogleCommand(user.GetAccountId(), id), ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        routes.MapDelete("/accounts/me/connections/microsoft/identities/{id}",
                async (string id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteMicrosoftCommand(user.GetAccountId(), id), ct);
                    return Results.NoContent();
                })
            .RequireAuthorization(Policy.RequireProfileAccess.Name);

        return routes;
    }
}
