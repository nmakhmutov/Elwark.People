using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands;
using People.Api.Application.Commands.Email;
using People.Api.Application.Commands.Password;
using People.Api.Application.Queries;
using People.Api.Mappers;
using People.Domain;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Infrastructure.Countries;
using People.Infrastructure.Timezones;
using AccountId = People.Grpc.Common.AccountId;
using Country = People.Grpc.Gateway.Country;
using EmailConnection = People.Domain.Aggregates.Account.Identities.EmailConnection;
using Timezone = People.Grpc.Gateway.Timezone;

namespace People.Api.Grpc
{
    public class GatewayService : Gateway.GatewayBase
    {
        private readonly IMediator _mediator;
        private readonly ICountryService _country;
        private readonly ITimezoneService _timezone;

        public GatewayService(IMediator mediator, ICountryService country, ITimezoneService timezone)
        {
            _mediator = mediator;
            _country = country;
            _timezone = timezone;
        }

        public override async Task<ProfileReply> GetProfile(AccountId request, ServerCallContext context)
        {
            var data = await _mediator.Send(new GetAccountByIdQuery(request.ToAccountId()), context.CancellationToken);
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<ProfileReply> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
        {
            var command = new UpdateProfileCommand(
                request.Id.ToAccountId(),
                request.FirstName,
                request.LastName,
                request.Nickname,
                request.PreferNickname,
                request.Bio,
                request.DateOfBirth.ToDateTime(),
                request.Gender.FromGrpc(),
                request.Language,
                request.Timezone,
                request.FirstDayOfWeek.ToDayOfWeek(),
                request.CountryCode,
                request.CityName ?? string.Empty
            );

            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.ToAccountId()));
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<Confirming> ConfirmingConnection(ConfirmingRequest request,
            ServerCallContext context)
        {
            var query = new GetAccountByIdQuery(request.Id.ToAccountId());
            var account = await _mediator.Send(query, context.CancellationToken);

            if (account is null)
            {
                context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
                return new Confirming();
            }

            if (account.GetIdentity(request.Identity.ToIdentityKey()) is EmailConnection connection)
            {
                var confirmationId = await _mediator.Send(
                    new SendConfirmationCommand(account.Id, connection.Identity, new Language(request.Language)),
                    context.CancellationToken
                );

                return new Confirming
                {
                    Id = confirmationId.ToString()
                };
            }

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new Confirming();
        }

        public override async Task<ProfileReply> ConfirmConnection(ConfirmRequest request, ServerCallContext context)
        {
            var command = new ConfirmIdentityCommand(
                request.Id.ToAccountId(),
                new ObjectId(request.Confirm.Id),
                request.Confirm.Code,
                request.Identity.ToIdentityKey()
            );

            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.ToAccountId()));
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<ProfileReply> ChangeEmailType(ChangeEmailTypeRequest request,
            ServerCallContext context)
        {
            var command = new ChangeEmailTypeCommand(
                request.Id.ToAccountId(),
                request.Email,
                request.Type.ToEmailType()
            );

            await _mediator.Send(command);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.ToAccountId()));
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<ProfileReply> DeleteConnection(DeleteConnectionRequest request,
            ServerCallContext context)
        {
            var command = new DeleteIdentityCommand(request.Id.ToAccountId(), request.Identity.ToIdentityKey());
            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.ToAccountId()));
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<Confirming> CreatingPassword(CreatingPasswordRequest request,
            ServerCallContext context)
        {
            var query = new GetAccountByIdQuery(request.Id.ToAccountId());
            var account = await _mediator.Send(query, context.CancellationToken);

            if (account is null)
            {
                context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
                return new Confirming();
            }

            if (account.IsPasswordAvailable())
            {
                context.Status = new Status(StatusCode.InvalidArgument, ElwarkExceptionCodes.PasswordAlreadyCreated);
                return new Confirming();
            }

            var confirmationId = await _mediator.Send(
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().GetIdentity(),
                    new Language(request.Language)),
                context.CancellationToken
            );

            return new Confirming
            {
                Id = confirmationId.ToString()
            };
        }

        public override async Task<ProfileReply> CreatePassword(CreatePasswordRequest request,
            ServerCallContext context)
        {
            var command = new CreatePasswordCommand(
                request.Id.ToAccountId(),
                new ObjectId(request.Confirm.Id),
                request.Confirm.Code,
                request.Password
            );

            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.ToAccountId()));
            if (data is not null)
                return data.ToGatewayProfileReply();

            context.Status = new Status(StatusCode.NotFound, ElwarkExceptionCodes.AccountNotFound);
            return new ProfileReply();
        }

        public override async Task<Empty> UpdatePassword(UpdatePasswordRequest request, ServerCallContext context)
        {
            await _mediator.Send(new UpdatePasswordCommand(request.Id.ToAccountId(), request.OldPassword,
                request.NewPassword), context.CancellationToken);

            return new Empty();
        }

        public override async Task<CountriesReply> GetCountries(CountriesRequest request, ServerCallContext context)
        {
            var result = await _country.GetAsync(new Language(request.Language), context.CancellationToken);

            return new CountriesReply
            {
                Countries =
                {
                    result.Select(x => new Country
                    {
                        Code = x.Alpha2Code,
                        Name = x.Name
                    })
                }
            };
        }

        public override async Task<TimezonesReply> GetTimezones(Empty request, ServerCallContext context)
        {
            var result = await _timezone.GetAsync(context.CancellationToken);

            return new TimezonesReply
            {
                Timezones =
                {
                    result.Select(x => new Timezone
                    {
                        Name = x.Name,
                        Offset = x.Offset.ToDuration()
                    })
                }
            };
        }

        public override async Task<Empty> SendEmail(SendEmailRequest request, ServerCallContext context)
        {
            await (request.IdentityCase switch
            {
                SendEmailRequest.IdentityOneofCase.Email =>
                    SendEmailAsync(request.Email, request.Subject, request.Body, context.CancellationToken),

                SendEmailRequest.IdentityOneofCase.Id =>
                    SendEmailAsync(request.Id.Value, request.Subject, request.Body, context.CancellationToken),

                _ => throw new ArgumentOutOfRangeException(nameof(request), "Unknown identity for send email message")
            });

            return new Empty();
        }

        private async Task SendEmailAsync(Domain.Aggregates.Account.AccountId id, string subject, string body,
            CancellationToken ct)
        {
            var account = await _mediator.Send(new GetAccountByIdQuery(id), ct);

            if (account is null)
                return;

            await SendEmailAsync(account.GetPrimaryEmail().Address, subject, body, ct);
        }

        private Task SendEmailAsync(string email, string subject, string body, CancellationToken ct) =>
            _mediator.Send(new AddEmailToQueueCommand(email, subject, body), ct);
    }
}
