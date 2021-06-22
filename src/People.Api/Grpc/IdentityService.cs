using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands;
using People.Api.Application.Commands.Attach;
using People.Api.Application.Commands.Password;
using People.Api.Application.Commands.SignIn;
using People.Api.Application.Commands.SignUp;
using People.Api.Application.Queries;
using People.Api.Infrastructure.Provider.Social.Google;
using People.Api.Infrastructure.Provider.Social.Microsoft;
using People.Api.Mappers;
using People.Domain;
using People.Domain.Aggregates.Account.Identities;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Identity;
using Identity = People.Grpc.Identity.Identity;

namespace People.Api.Grpc
{
    public sealed class IdentityService : Identity.IdentityBase
    {
        private readonly IGoogleApiService _google;
        private readonly Language _language;
        private readonly IMediator _mediator;
        private readonly IMicrosoftApiService _microsoft;

        public IdentityService(IMediator mediator, IGoogleApiService google, IMicrosoftApiService microsoft)
        {
            _mediator = mediator;
            _google = google;
            _microsoft = microsoft;
            _language = new Language(CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        }

        public override async Task<AccountReply> GetAccountById(AccountId request, ServerCallContext context)
        {
            var data = await _mediator.Send(new GetAccountByIdQuery(request.Value), context.CancellationToken);
            if (data is not null)
                return data.ToIdentityAccountReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new AccountReply();
        }

        public override async Task<StatusReply> GetStatus(AccountId request, ServerCallContext context)
        {
            var query = new GetAccountStatusQuery(request.ToAccountId());
            var data = await _mediator.Send(query);

            return new StatusReply
            {
                IsActive = data.IsActive
            };
        }

        public override async Task<SignInReply> SignInByEmail(SignInByEmailRequest request, ServerCallContext context)
        {
            var command = new SignInByEmailCommand(
                new EmailIdentity(request.Email),
                request.Password,
                ParseIpAddress(request.Ip)
            );
            var data = await _mediator.Send(command, context.CancellationToken);

            return data.ToSignInReply();
        }

        public override async Task<SignInReply> SignInByGoogle(SignInByProviderRequest request,
            ServerCallContext context)
        {
            var google = await _google.GetAsync(request.AccessToken, context.CancellationToken);
            var command = new SignInByGoogleCommand(google.Identity, ParseIpAddress(request.Ip));
            var data = await _mediator.Send(command, context.CancellationToken);

            return data.ToSignInReply();
        }

        public override async Task<SignInReply> SignInByMicrosoft(SignInByProviderRequest request,
            ServerCallContext context)
        {
            var microsoft = await _microsoft.GetAsync(request.AccessToken, context.CancellationToken);
            var command = new SignInByMicrosoftCommand(microsoft.Identity, ParseIpAddress(request.Ip));
            var data = await _mediator.Send(command, context.CancellationToken);

            return data.ToSignInReply();
        }

        public override async Task<SignUpReply> SignUpByEmail(SignUpByEmailRequest request, ServerCallContext context)
        {
            var command = new SignUpByEmailCommand(
                new EmailIdentity(request.Email),
                request.Password,
                _language,
                ParseIpAddress(request.Ip)
            );
            var data = await _mediator.Send(command);

            return data.ToSignUpReply();
        }

        public override async Task<SignUpReply> SignUpByGoogle(SignUpByProviderRequest request,
            ServerCallContext context)
        {
            var google = await _google.GetAsync(request.AccessToken, context.CancellationToken);
            var command = new SignUpByGoogleCommand(
                google.Identity,
                google.Email,
                google.FirstName,
                google.LastName,
                google.Picture,
                google.IsEmailVerified,
                _language,
                ParseIpAddress(request.Ip)
            );
            var data = await _mediator.Send(command);

            return data.ToSignUpReply();
        }

        public override async Task<SignUpReply> SignUpByMicrosoft(SignUpByProviderRequest request,
            ServerCallContext context)
        {
            var microsoft = await _microsoft.GetAsync(request.AccessToken, context.CancellationToken);
            var command = new SignUpByMicrosoftCommand(
                microsoft.Identity,
                microsoft.Email,
                microsoft.FirstName,
                microsoft.LastName,
                _language,
                ParseIpAddress(request.Ip)
            );
            var data = await _mediator.Send(command);

            return data.ToSignUpReply();
        }

        public override async Task<AccountId> IsConfirmationAvailable(Confirming request, ServerCallContext context)
        {
            var query = new CheckSignUpConfirmationQuery(new ObjectId(request.Id));
            var confirmation = await _mediator.Send(query, context.CancellationToken);

            return confirmation.AccountId.ToAccountId();
        }

        public override async Task<Empty> ConfirmSignUp(ConfirmSignUpRequest request, ServerCallContext context)
        {
            var command = new ConfirmIdentityCommand(
                request.Id.ToAccountId(),
                new ObjectId(request.Confirm.Id),
                request.Confirm.Code
            );
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<Confirming> ResendSignUpConfirmation(AccountId request, ServerCallContext context)
        {
            var command = new ResendSignUpConfirmationCommand(request.ToAccountId(), _language);
            var confirmationId = await _mediator.Send(command, context.CancellationToken);

            return new Confirming
            {
                Id = confirmationId.ToString()
            };
        }

        public override async Task<Empty> AttachEmail(AttachRequest request, ServerCallContext context)
        {
            var command = new AttachEmailCommand(request.Id.ToAccountId(), new EmailIdentity(request.Value));
            await _mediator.Send(command, context.CancellationToken);

            return new Empty();
        }

        public override async Task<Empty> AttachGoogle(AttachRequest request, ServerCallContext context)
        {
            var google = await _google.GetAsync(request.Value, context.CancellationToken);
            var command = new AttachGoogleCommand(
                request.Id.ToAccountId(),
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
                request.Id.ToAccountId(),
                microsoft.Identity,
                microsoft.Email,
                microsoft.FirstName,
                microsoft.LastName
            );

            await _mediator.Send(command, context.CancellationToken);

            return new Empty();
        }

        public override async Task<AccountId> ResetPassword(People.Grpc.Common.Identity request,
            ServerCallContext context)
        {
            var command = new ResetPasswordCommand(request.ToIdentityKey(), _language);
            var data = await _mediator.Send(command, context.CancellationToken);

            return data.ToAccountId();
        }

        public override async Task<Empty> RestorePassword(RestorePasswordRequest request, ServerCallContext context)
        {
            var command = new CreatePasswordCommand(
                request.Id.ToAccountId(),
                new ObjectId(request.Confirm.Id),
                request.Confirm.Code,
                request.Password
            );
            await _mediator.Send(command);

            return new Empty();
        }

        private static IPAddress ParseIpAddress(string? ip) =>
            IPAddress.TryParse(ip, out var value) ? value : IPAddress.None;
    }
}
