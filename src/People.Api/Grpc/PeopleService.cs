using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using People.Api.Application.Commands.BanAccount;
using People.Api.Application.Commands.ChangePrimaryEmail;
using People.Api.Application.Commands.CheckConfirmation;
using People.Api.Application.Commands.ConfirmConnection;
using People.Api.Application.Commands.ConfuteConnection;
using People.Api.Application.Commands.CreatePassword;
using People.Api.Application.Commands.CreateRole;
using People.Api.Application.Commands.DeleteAccount;
using People.Api.Application.Commands.DeleteConnection;
using People.Api.Application.Commands.DeleteRole;
using People.Api.Application.Commands.SendConfirmation;
using People.Api.Application.Commands.UnbanAccount;
using People.Api.Application.Commands.UpdateAccount;
using People.Api.Application.Commands.UpdatePassword;
using People.Api.Application.Queries.GetAccountById;
using People.Api.Application.Queries.GetAccounts;
using People.Api.Mappers;
using People.Domain;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;
using People.Grpc.Common;
using People.Grpc.Gateway;
using People.Infrastructure.Countries;
using EmailConnection = People.Domain.Aggregates.AccountAggregate.Connections.EmailConnection;

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

    public override async Task<AccountsReply> GetAccounts(AccountsRequest request, ServerCallContext context)
    {
        var query = new GetAccountsQuery(request.Page, request.Limit);
        var (items, pages, count) = await _mediator.Send(query, context.CancellationToken);

        return new AccountsReply
        {
            Count = count,
            Pages = pages,
            Topics = { items.Select(x => x.ToGrpc()) }
        };
    }

    public override async Task<AccountReply> GetAccount(AccountIdValue request, ServerCallContext context)
    {
        var account = await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()), context.CancellationToken);
        return account.ToGrpc();
    }

    public override async Task<AccountReply> UpdateAccount(UpdateAccountRequest request, ServerCallContext context)
    {
        var command = new UpdateAccountCommand(
            request.Id.FromGrpc(),
            request.FirstName,
            request.LastName,
            request.Nickname,
            request.PreferNickname,
            request.Language,
            request.TimeZone,
            request.DateFormat,
            request.TimeFormat,
            request.WeekStart.FromGrpc(),
            request.CountryCode
        );

        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> ConfirmConnection(ConfirmRequest request, ServerCallContext context)
    {
        if (request.Confirm is not null)
            await _mediator.Send(
                new CheckConfirmationCommand(request.Confirm.Token, request.Confirm.Code),
                context.CancellationToken
            );

        await _mediator.Send(
            new ConfirmConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc()),
            context.CancellationToken
        );

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> ConfuteConnection(ConfuteRequest request, ServerCallContext context)
    {
        var command = new ConfuteConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> DeleteConnection(DeleteConnectionRequest request,
        ServerCallContext context)
    {
        var command = new DeleteConnectionCommand(request.Id.FromGrpc(), request.Identity.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> ChangePrimaryEmail(ChangePrimaryEmailRequest request,
        ServerCallContext context)
    {
        var command = new ChangePrimaryEmailCommand(request.Id.FromGrpc(), new EmailIdentity(request.Email));
        await _mediator.Send(command);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> CreatePassword(CreatePasswordRequest request, ServerCallContext context)
    {
        var command = new CheckConfirmationCommand(request.Confirm.Token, request.Confirm.Code);
        await _mediator.Send(command, context.CancellationToken);

        await _mediator
            .Send(new CreatePasswordCommand(request.Id.FromGrpc(), request.Password), context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<Empty> UpdatePassword(UpdatePasswordRequest request, ServerCallContext context)
    {
        var command = new UpdatePasswordCommand(request.Id.FromGrpc(), request.OldPassword, request.NewPassword);
        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    public override async Task<AccountReply> CreateRole(RoleRequest request, ServerCallContext context)
    {
        var command = new CreateRoleCommand(request.Id.FromGrpc(), request.Role);
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> DeleteRole(RoleRequest request, ServerCallContext context)
    {
        var command = new DeleteRoleCommand(request.Id.FromGrpc(), request.Role);
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> Ban(BanRequest request, ServerCallContext context)
    {
        var command = new BanAccountCommand(request.Id.FromGrpc(), request.Reason, request.ExpiredAt?.ToDateTime());
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<AccountReply> Unban(AccountIdValue request, ServerCallContext context)
    {
        var command = new UnbanAccountCommand(request.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        var account = await _mediator.Send(new GetAccountByIdQuery(request.FromGrpc()));
        return account.ToGrpc();
    }

    public override async Task<Empty> Delete(AccountIdValue request, ServerCallContext context)
    {
        var command = new DeleteAccountCommand(request.FromGrpc());
        await _mediator.Send(command, context.CancellationToken);

        return new Empty();
    }

    public override async Task<ConfirmationCodeReply> SendConfirmationCode(ConfirmationCodeRequest request,
        ServerCallContext context)
    {
        var account = await _mediator.Send(new GetAccountByIdQuery(request.Id.FromGrpc()), context.CancellationToken);
        var identity = request.Identity is null
            ? account.GetPrimaryEmail()
            : account.GetIdentity(request.Identity.FromGrpc());

        if (identity is not EmailConnection email)
            throw new PeopleException(ExceptionCodes.IdentityNotFound);

        var token = await _mediator
            .Send(new SendConfirmationCommand(account.Id, email.Identity, Language.Parse(request.Language)));

        return new ConfirmationCodeReply
        {
            Token = token
        };
    }

    public override async Task<CountriesReply> GetCountries(CountriesRequest request, ServerCallContext context)
    {
        var result = await _country.GetAsync(Language.Parse(request.Language), context.CancellationToken);

        return new CountriesReply
        {
            Countries =
            {
                result.Select(x => new CountriesReply.Types.Country
                {
                    Code = x.Alpha2Code,
                    Name = x.Name
                })
            }
        };
    }
}
