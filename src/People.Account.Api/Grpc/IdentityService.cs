using System.Net;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Account.Api.Application.Commands.AttachEmail;
using People.Account.Api.Application.Commands.AttachGoogle;
using People.Account.Api.Application.Commands.AttachMicrosoft;
using People.Account.Api.Application.Commands.CheckConfirmation;
using People.Account.Api.Application.Commands.ConfirmConnection;
using People.Account.Api.Application.Commands.CreatePassword;
using People.Account.Api.Application.Commands.SendConfirmation;
using People.Account.Api.Application.Commands.SignInByEmail;
using People.Account.Api.Application.Commands.SignInByGoogle;
using People.Account.Api.Application.Commands.SignInByMicrosoft;
using People.Account.Api.Application.Commands.SignUpByEmail;
using People.Account.Api.Application.Commands.SignUpByGoogle;
using People.Account.Api.Application.Commands.SignUpByMicrosoft;
using People.Account.Api.Application.Queries.CheckSignUpConfirmation;
using People.Account.Api.Application.Queries.GetAccountById;
using People.Account.Api.Application.Queries.GetAccountByIdentity;
using People.Account.Api.Application.Queries.GetAccountStatus;
using People.Account.Api.Infrastructure.Provider.Social.Google;
using People.Account.Api.Infrastructure.Provider.Social.Microsoft;
using People.Account.Api.Mappers;
using People.Account.Domain;
using People.Account.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Identity;
using Identity = People.Grpc.Identity.Identity;

namespace People.Account.Api.Grpc
{
    public sealed partial class IdentityService : Identity.IdentityBase
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

        public override async Task<AccountReply> GetAccountById(AccountId request, ServerCallContext context)
        {
            var data = await _mediator.Send(new GetAccountByIdQuery(request.Value), context.CancellationToken);

            return ToAccountReply(data);
        }

        public override async Task<StatusReply> GetStatus(AccountId request, ServerCallContext context)
        {
            var query = new GetAccountStatusQuery(request.FromGrpc());
            var data = await _mediator.Send(query);

            return new StatusReply
            {
                IsActive = data.IsActive
            };
        }

        public override async Task<SignInReply> SignInByEmail(SignInByEmailRequest request, ServerCallContext context)
        {
            var command = new SignInByEmailCommand(
                new Domain.Aggregates.AccountAggregate.Identities.Identity.Email(request.Email),
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
                    new Domain.Aggregates.AccountAggregate.Identities.Identity.Email(request.Email),
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

        public override async Task<AccountId> IsConfirmationAvailable(Confirming request, ServerCallContext context)
        {
            var query = new CheckSignUpConfirmationQuery(new ObjectId(request.Id));
            var confirmation = await _mediator.Send(query, context.CancellationToken);

            return confirmation.AccountId.ToGrpc();
        }

        public override async Task<Empty> ConfirmSignUp(ConfirmSignUpRequest request, ServerCallContext context)
        {
            await _mediator.Send(
                new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
                context.CancellationToken
            );
            
            var command = new ConfirmConnectionCommand(request.Id.FromGrpc());
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<Confirming> ResendSignUpConfirmation(ResendSignUpConfirmationRequest request,
            ServerCallContext context)
        {
            var account =
                await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()), context.CancellationToken);

            if (account.IsConfirmed())
                throw new ElwarkException(ElwarkExceptionCodes.IdentityAlreadyConfirmed);

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
                    request.Id.FromGrpc(),
                    new Domain.Aggregates.AccountAggregate.Identities.Identity.Email(request.Value)
                ),
                context.CancellationToken
            );

            return new Empty();
        }

        public override async Task<Empty> AttachGoogle(AttachRequest request, ServerCallContext context)
        {
            var google = await _google.GetAsync(request.Value, context.CancellationToken);
            var command = new AttachGoogleCommand(
                request.Id.FromGrpc(),
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
                request.Id.FromGrpc(),
                microsoft.Identity,
                microsoft.Email,
                microsoft.FirstName,
                microsoft.LastName
            );

            await _mediator.Send(command, context.CancellationToken);

            return new Empty();
        }

        public override async Task<AccountId> ResetPassword(ResetPasswordRequest request, ServerCallContext context)
        {
            var account =
                await _mediator.Send(new GetAccountByIdentityQuery(request.Identity.FromGrpc()),
                    context.CancellationToken);

            if (!account.IsPasswordAvailable())
                throw new ElwarkException(ElwarkExceptionCodes.PasswordNotCreated);

            await _mediator.Send(
                new SendConfirmationCommand(
                    account.Id,
                    account.GetPrimaryEmail().Identity,
                    new Language(request.Language)
                ),
                context.CancellationToken
            );

            return account.Id.ToGrpc();
        }

        public override async Task<Empty> RestorePassword(RestorePasswordRequest request, ServerCallContext context)
        {
            await _mediator.Send(
                new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
                context.CancellationToken
            );

            await _mediator.Send(new CreatePasswordCommand(request.Id.FromGrpc(), request.Password));

            return new Empty();
        }

        private static IPAddress ParseIpAddress(string? ip) =>
            IPAddress.TryParse(ip, out var value) ? value : IPAddress.None;
    }
}
