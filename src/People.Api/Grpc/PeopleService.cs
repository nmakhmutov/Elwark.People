using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using MongoDB.Bson;
using People.Api.Application.Commands.CheckConfirmation;
using People.Api.Application.Commands.ConfirmConnection;
using People.Api.Application.Commands.CreatePassword;
using People.Api.Application.Commands.DeleteConnection;
using People.Api.Application.Commands.SendConfirmation;
using People.Api.Application.Commands.SetAsPrimaryEmail;
using People.Api.Application.Commands.UpdatePassword;
using People.Api.Application.Commands.UpdateProfile;
using People.Api.Application.Queries.GetAccountById;
using People.Api.Mappers;
using People.Domain;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Infrastructure.Countries;
using Country = People.Grpc.Gateway.Country;
using EmailConnection = People.Domain.Aggregates.AccountAggregate.Identities.EmailConnection;
using Identity = People.Domain.Aggregates.AccountAggregate.Identities.Identity;

namespace People.Api.Grpc;

internal sealed class PeopleService : People.Grpc.Gateway.PeopleService.PeopleServiceBase
{
    private readonly ICountryService _country;
    private readonly IMediator _mediator;

    public PeopleService(IMediator mediator, ICountryService country)
    {
        _mediator = mediator;
        _country = country;
    }

    public override async Task<ProfileReply> GetProfile(AccountId request, ServerCallContext context)
    {
        var account = await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()), context.CancellationToken);
        return account.ToProfileReply();
    }

    public override async Task<EmailNotificationInformation> GetEmailNotification(AccountId request,
        ServerCallContext context)
    {
        var data = await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()), context.CancellationToken);
        var email = data.GetPrimaryEmail();

        return new EmailNotificationInformation
        {
            PrimaryEmail = email.Value,
            TimeZone = data.TimeZone
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
            request.Language,
            request.TimeZone,
            request.FirstDayOfWeek.FromGrpc(),
            request.CountryCode
        );

        await _mediator.Send(command, context.CancellationToken);

        var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return data.ToProfileReply();
    }

    public override async Task<Confirming> ConfirmingConnection(ConfirmingRequest request, ServerCallContext context)
    {
        var query = new GetAccountByIdQuery(request.Id.FromGrpc());
        var account = await _mediator.Send(query, context.CancellationToken);

        if (account.GetIdentity(request.Identity.FromGrpc()) is EmailConnection connection)
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
        return data.ToProfileReply();
    }

    public override async Task<ProfileReply> SetEmailAsPrimary(SetEmailAsPrimaryRequest request,
        ServerCallContext context)
    {
        var command = new SetAsPrimaryEmailCommand(request.Id.FromGrpc(), new Identity.Email(request.Email));

        await _mediator.Send(command);

        var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return data.ToProfileReply();
    }

    public override async Task<ProfileReply> DeleteConnection(DeleteConnectionRequest request,
        ServerCallContext context)
    {
        var command = new DeleteConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return data.ToProfileReply();
    }

    public override async Task<Confirming> CreatingPassword(CreatingPasswordRequest request, ServerCallContext context)
    {
        var query = new GetAccountByIdQuery(request.Id.FromGrpc());
        var account = await _mediator.Send(query, context.CancellationToken);

        if (account.IsPasswordAvailable())
        {
            context.Status = new Status(StatusCode.InvalidArgument, ElwarkExceptionCodes.PasswordAlreadyCreated);
            return new Confirming();
        }

        var command = new SendConfirmationCommand(account.Id, account.GetPrimaryEmail().Identity,
            new Language(request.Language));
        var confirmationId = await _mediator.Send(command, context.CancellationToken);

        return new Confirming
        {
            Id = confirmationId.ToString()
        };
    }

    public override async Task<ProfileReply> CreatePassword(CreatePasswordRequest request, ServerCallContext context)
    {
        var command = new CheckConfirmationCommand(new ObjectId(request.Confirm.Id), request.Confirm.Code);
        await _mediator.Send(command, context.CancellationToken);

        await _mediator.Send(
            new CreatePasswordCommand(request.Id.FromGrpc(), request.Password),
            context.CancellationToken
        );

        var data = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return data.ToProfileReply();
    }

    public override async Task<Empty> UpdatePassword(UpdatePasswordRequest request, ServerCallContext context)
    {
        var command = new UpdatePasswordCommand(request.Id.FromGrpc(), request.OldPassword, request.NewPassword);
        await _mediator.Send(command, context.CancellationToken);

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
