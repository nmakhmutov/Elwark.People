using System;
using System.Net;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Commands;
using People.Api.Application.Queries;
using People.Api.Infrastructure.Providers.Google;
using People.Api.Infrastructure.Providers.Microsoft;
using People.Api.Mappers;
using People.Domain.AggregateModels.Account;
using People.Domain.AggregateModels.Account.Identities;
using People.Domain.Exceptions;
using People.Grpc.Identity;
using AccountId = People.Grpc.Common.AccountId;
using Identity = People.Grpc.Identity.Identity;

namespace People.Api.Grpc
{
    public sealed class IdentityService : Identity.IdentityBase
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
                ParseLanguage(request.Language),
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
                ParseLanguage(google.Locale?.TwoLetterISOLanguageName ?? request.Language),
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
                ParseLanguage(request.Language),
                ParseIpAddress(request.Ip)
            );
            var data = await _mediator.Send(command);

            return data.ToSignUpReply();
        }

        public override async Task<CheckSignUpConfirmationReply> CheckSignUpConfirmation(
            AccountId request, ServerCallContext context)
        {
            var query = new CheckSignUpConfirmationQuery(request.ToAccountId());
            var confirmation = await _mediator.Send(query, context.CancellationToken);

            return new CheckSignUpConfirmationReply
            {
                Key = confirmation.Key.ToIdentityKey(),
                CreatedAt = confirmation.CreatedAt.ToTimestamp(),
                ExpireAt = confirmation.ExpireAt.ToTimestamp()
            };
        }

        public override async Task<Empty> ResendSignUpConfirmation(AccountId request,
            ServerCallContext context)
        {
            var query = new GetAccountByIdQuery(request.ToAccountId());
            var account = await _mediator.Send(query, context.CancellationToken);
            if (account is null)
            {
                context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
                return new Empty();
            }

            if (account.IsConfirmed())
            {
                context.Status = new Status(StatusCode.FailedPrecondition,
                    ElwarkExceptionCodes.IdentityAlreadyConfirmed);
                return new Empty();
            }

            var command = new SendPrimaryEmailConfirmationCommand(account.Id, account.GetPrimaryEmail());
            await _mediator.Send(command, context.CancellationToken);

            return new Empty();
        }

        public override async Task<AccountId> ResetPassword(People.Grpc.Common.Identity request,
            ServerCallContext context)
        {
            var command = new ResetPasswordCommand(request.ToIdentityKey());
            var data = await _mediator.Send(command, context.CancellationToken);

            return data.ToAccountId();
        }

        public override async Task<Empty> ConfirmSignUp(ConfirmSignUpRequest request, ServerCallContext context)
        {
            var command = new ConfirmEmailSignUpCommand(request.Id.ToAccountId(), request.Code);
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<Empty> RestorePassword(RestorePasswordRequest request, ServerCallContext context)
        {
            var command = new RestorePasswordCommand(request.Id.ToAccountId(), request.Code, request.Password);
            await _mediator.Send(command);

            return new Empty();
        }

        private static IPAddress ParseIpAddress(string? ip) =>
            IPAddress.TryParse(ip, out var value) ? value : IPAddress.None;

        private static Language ParseLanguage(string? language)
        {
            if (string.IsNullOrEmpty(language))
                return Language.Default;

            return string.Equals("iv", language, StringComparison.InvariantCultureIgnoreCase)
                ? Language.Default
                : new Language(language);
        }
    }
}