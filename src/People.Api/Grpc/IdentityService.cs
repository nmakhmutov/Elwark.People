using System.Net;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands.AttachEmail;
using People.Api.Application.Commands.AttachGoogle;
using People.Api.Application.Commands.AttachMicrosoft;
using People.Api.Application.Commands.CheckConfirmation;
using People.Api.Application.Commands.ConfirmConnection;
using People.Api.Application.Commands.CreatePassword;
using People.Api.Application.Commands.SendConfirmation;
using People.Api.Application.Commands.SignInByEmail;
using People.Api.Application.Commands.SignInByGoogle;
using People.Api.Application.Commands.SignInByMicrosoft;
using People.Api.Application.Commands.SignUpByEmail;
using People.Api.Application.Commands.SignUpByGoogle;
using People.Api.Application.Commands.SignUpByMicrosoft;
using People.Api.Application.Queries.CheckSignUpConfirmation;
using People.Api.Application.Queries.GetAccountById;
using People.Api.Application.Queries.GetAccountByIdentity;
using People.Api.Application.Queries.GetAccountStatus;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Api.Mappers;
using People.Domain;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Identity;
using Identity = People.Domain.Aggregates.AccountAggregate.Identities.Identity;

namespace People.Api.Grpc;

internal sealed partial class IdentityService : People.Grpc.Identity.IdentityService.IdentityServiceBase
{
    private readonly IGoogleApiService _google;
    private readonly IMediator _mediator;
    private readonly IMicrosoftApiService _microsoft;

    public IdentityService(IMediator mediator, IGoogleApiService google, IMicrosoftApiService microsoft)
    {
        _mediator = mediator;
        _google = google;
        _microsoft = microsoft;
    }

    public override async Task<AccountReply> GetAccountById(AccountIdValue request, ServerCallContext context)
    {
        var data = await _mediator.Send(new GetAccountByIdQuery(request), context.CancellationToken);

        return ToAccountReply(data);
    }

    public override async Task<StatusReply> GetStatus(AccountIdValue request, ServerCallContext context)
    {
        var query = new GetAccountStatusQuery(request);
        var data = await _mediator.Send(query);

        return new StatusReply
        {
            IsActive = data.IsActive
        };
    }

    public override async Task<SignInReply> SignInByEmail(SignInByEmailRequest request, ServerCallContext context)
    {
        var command = new SignInByEmailCommand(
            new Identity.Email(request.Email),
            request.Password,
            ParseIpAddress(request.Ip)
        );

        var (accountId, fullName) = await _mediator.Send(command, context.CancellationToken);

        return ToSignInReply(accountId, fullName);
    }

    public override async Task<SignInReply> SignInByGoogle(SignInBySocialRequest request, ServerCallContext context)
    {
        var google = await _google.GetAsync(request.AccessToken, context.CancellationToken);
        var command = new SignInByGoogleCommand(google.Identity, ParseIpAddress(request.Ip));
        var (accountId, fullName) = await _mediator.Send(command, context.CancellationToken);

        return ToSignInReply(accountId, fullName);
    }

    public override async Task<SignInReply> SignInByMicrosoft(SignInBySocialRequest request,
        ServerCallContext context)
    {
        var microsoft = await _microsoft.GetAsync(request.AccessToken, context.CancellationToken);
        var command = new SignInByMicrosoftCommand(microsoft.Identity, ParseIpAddress(request.Ip));
        var (accountId, fullName) = await _mediator.Send(command, context.CancellationToken);

        return ToSignInReply(accountId, fullName);
    }

    public override async Task<SignUpReply> SignUpByEmail(SignUpByEmailRequest request, ServerCallContext context)
    {
        var language = new Language(request.Language);
        var (id, fullName, emailConnection) = await _mediator.Send(
            new SignUpByEmailCommand(
                new Identity.Email(request.Email),
                request.Password,
                language,
                ParseIpAddress(request.Ip)
            ),
            context.CancellationToken
        );

        if (emailConnection.IsConfirmed)
            return ToSignUpReply(id, fullName);

        var confirmationId = await _mediator.Send(
            new SendConfirmationCommand(id, emailConnection.Identity, language),
            context.CancellationToken
        );

        return ToSignUpReply(id, fullName, confirmationId);
    }

    public override async Task<SignUpReply> SignUpByGoogle(SignUpBySocialRequest request, ServerCallContext context)
    {
        var google = await _google.GetAsync(request.AccessToken, context.CancellationToken);
        var command = new SignUpByGoogleCommand(
            google.Identity,
            google.Email,
            google.FirstName,
            google.LastName,
            google.Picture,
            google.IsEmailVerified,
            new Language(request.Language),
            ParseIpAddress(request.Ip)
        );

        var (accountId, fullName, _) = await _mediator.Send(command);

        return ToSignUpReply(accountId, fullName);
    }

    public override async Task<SignUpReply> SignUpByMicrosoft(SignUpBySocialRequest request,
        ServerCallContext context)
    {
        var microsoft = await _microsoft.GetAsync(request.AccessToken, context.CancellationToken);
        var command = new SignUpByMicrosoftCommand(
            microsoft.Identity,
            microsoft.Email,
            microsoft.FirstName,
            microsoft.LastName,
            new Language(request.Language),
            ParseIpAddress(request.Ip)
        );

        var (accountId, fullName, _) = await _mediator.Send(command);

        return ToSignUpReply(accountId, fullName);
    }

    public override async Task<AccountIdValue> IsConfirmationAvailable(Confirming request, ServerCallContext context)
    {
        var query = new CheckSignUpConfirmationQuery(new ObjectId(request.Id));
        var confirmation = await _mediator.Send(query, context.CancellationToken);

        return confirmation.AccountId;
    }

    public override async Task<Empty> ConfirmSignUp(ConfirmSignUpRequest request, ServerCallContext context)
    {
        await _mediator.Send(
            new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
            context.CancellationToken
        );

        var command = new ConfirmConnectionCommand(request.Id);
        await _mediator.Send(command);

        return new Empty();
    }

    public override async Task<Confirming> ResendSignUpConfirmation(ResendSignUpConfirmationRequest request,
        ServerCallContext context)
    {
        var account =
            await _mediator.Send(new GetAccountByIdQuery(request.Id), context.CancellationToken);

        if (account.IsConfirmed())
            throw new PeopleException(ExceptionCodes.IdentityAlreadyConfirmed);

        var confirmationId = await _mediator.Send(
            new SendConfirmationCommand(
                account.Id,
                account.GetPrimaryEmail().Identity,
                new Language(request.Language)
            ),
            context.CancellationToken
        );

        return new Confirming
        {
            Id = confirmationId.ToString()
        };
    }

    public override async Task<Empty> AttachEmail(AttachRequest request, ServerCallContext context)
    {
        await _mediator.Send(
            new AttachEmailCommand(
                request.Id,
                new Identity.Email(request.Value)
            ),
            context.CancellationToken
        );

        return new Empty();
    }

    public override async Task<Empty> AttachGoogle(AttachRequest request, ServerCallContext context)
    {
        var google = await _google.GetAsync(request.Value, context.CancellationToken);
        var command = new AttachGoogleCommand(
            request.Id,
            google.Identity,
            google.Email,
            google.FirstName,
            google.LastName,
            google.Picture,
            google.IsEmailVerified
        );

        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    public override async Task<Empty> AttachMicrosoft(AttachRequest request, ServerCallContext context)
    {
        var microsoft = await _microsoft.GetAsync(request.Value, context.CancellationToken);
        var command = new AttachMicrosoftCommand(
            request.Id,
            microsoft.Identity,
            microsoft.Email,
            microsoft.FirstName,
            microsoft.LastName
        );

        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    public override async Task<AccountIdValue> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
    {
        var account =
            await _mediator.Send(new GetAccountByIdentityQuery(request.Identity.FromGrpc()),
                context.CancellationToken);

        if (!account.IsPasswordAvailable())
            throw new PeopleException(ExceptionCodes.PasswordNotCreated);

        await _mediator.Send(
            new SendConfirmationCommand(
                account.Id,
                account.GetPrimaryEmail().Identity,
                new Language(request.Language)
            ),
            context.CancellationToken
        );

        return account.Id;
    }

    public override async Task<Empty> RestorePassword(RestorePasswordRequest request, ServerCallContext context)
    {
        await _mediator.Send(
            new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
            context.CancellationToken
        );

        await _mediator.Send(new CreatePasswordCommand(request.Id, request.Password));

        return new Empty();
    }

    private static IPAddress ParseIpAddress(string? ip) =>
        IPAddress.TryParse(ip, out var value) ? value : IPAddress.None;
}
