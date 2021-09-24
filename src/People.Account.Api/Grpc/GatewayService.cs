using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Account.Api.Application.Commands.CheckConfirmation;
using People.Account.Api.Application.Commands.ConfirmConnection;
using People.Account.Api.Application.Commands.CreatePassword;
using People.Account.Api.Application.Commands.DeleteConnection;
using People.Account.Api.Application.Commands.SendConfirmation;
using People.Account.Api.Application.Commands.SetAsPrimaryEmail;
using People.Account.Api.Application.Commands.UpdatePassword;
using People.Account.Api.Application.Commands.UpdateProfile;
using People.Account.Api.Application.Queries.GetAccountById;
using People.Account.Api.Mappers;
using People.Account.Domain;
using People.Account.Infrastructure.Countries;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Gateway;
using Country = People.Grpc.Gateway.Country;
using Identity = People.Account.Domain.Aggregates.AccountAggregate.Identities.Identity;

namespace People.Account.Api.Grpc
{
    public class GatewayService : Gateway.GatewayBase
    {
        private readonly ICountryService _country;
        private readonly IMediator _mediator;

        public GatewayService(IMediator mediator, ICountryService country)
        {
            _mediator = mediator;
            _country = country;
        }

        public override async Task<ProfileReply> GetProfile(AccountId request, ServerCallContext context)
        {
            var account =
                await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()), context.CancellationToken);
            return account.ToGatewayProfileReply();
        }

        public override async Task<Connection> GetPrimaryEmail(AccountId request, ServerCallContext context)
        {
            var data = await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()), context.CancellationToken);
            var email = data.GetPrimaryEmail();

            return new Connection
            {
                Type = email.Type.ToIdentityType(),
                Value = email.Value,
                IsConfirmed = email.IsConfirmed,
                Email = new EmailConnection
                {
                    IsPrimary = email.IsPrimary
                }
            };
        }

        public override async Task<ProfileReply> UpdateProfile(UpdateProfileRequest request, ServerCallContext context)
        {
            var command = new UpdateProfileCommand(
                request.Id.FromGrpc(),
                request.FirstName,
                request.LastName,
                request.Nickname,
                request.PreferNickname,
                request.Bio,
                request.DateOfBirth.ToDateTime(),
                request.Gender.FromGrpc(),
                request.Language,
                request.TimeZone,
                request.FirstDayOfWeek.ToDayOfWeek(),
                request.CountryCode,
                request.CityName ?? string.Empty
            );

            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
            return data.ToGatewayProfileReply();
        }

        public override async Task<Confirming> ConfirmingConnection(ConfirmingRequest request,
            ServerCallContext context)
        {
            var query = new GetAccountByIdQuery(request.Id.FromGrpc());
            var account = await _mediator.Send(query, context.CancellationToken);

            if (account.GetIdentity(request.Identity.FromGrpc()) is
                Domain.Aggregates.AccountAggregate.Identities.EmailConnection connection)
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
            await _mediator.Send(
                new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
                context.CancellationToken
            );

            await _mediator.Send(
                new ConfirmConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc()),
                context.CancellationToken
            );

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
            return data.ToGatewayProfileReply();
        }

        public override async Task<ProfileReply> SetEmailAsPrimary(SetEmailAsPrimaryRequest request,
            ServerCallContext context)
        {
            var command = new SetAsPrimaryEmailCommand(request.Id.FromGrpc(), new Identity.Email(request.Email));

            await _mediator.Send(command);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
            return data.ToGatewayProfileReply();
        }

        public override async Task<ProfileReply> DeleteConnection(DeleteConnectionRequest request,
            ServerCallContext context)
        {
            var command = new DeleteConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc());
            await _mediator.Send(command, context.CancellationToken);

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
            return data.ToGatewayProfileReply();
        }

        public override async Task<Confirming> CreatingPassword(CreatingPasswordRequest request,
            ServerCallContext context)
        {
            var query = new GetAccountByIdQuery(request.Id.FromGrpc());
            var account = await _mediator.Send(query, context.CancellationToken);

            if (account.IsPasswordAvailable())
            {
                context.Status = new Status(StatusCode.InvalidArgument, ElwarkExceptionCodes.PasswordAlreadyCreated);
                return new Confirming();
            }

            var confirmationId = await _mediator.Send(
                new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().Identity,
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
            await _mediator.Send(
                new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code),
                context.CancellationToken
            );

            await _mediator.Send(
                new CreatePasswordCommand(request.Id.FromGrpc(), request.Password),
                context.CancellationToken
            );

            var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
            return data.ToGatewayProfileReply();
        }

        public override async Task<Empty> UpdatePassword(UpdatePasswordRequest request, ServerCallContext context)
        {
            await _mediator.Send(new UpdatePasswordCommand(request.Id.FromGrpc(), request.OldPassword,
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
    }
}
