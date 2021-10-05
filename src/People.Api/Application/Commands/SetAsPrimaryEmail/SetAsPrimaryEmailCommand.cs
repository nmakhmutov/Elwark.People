using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.SetAsPrimaryEmail;

internal sealed record SetAsPrimaryEmailCommand(AccountId Id, Identity.Email Email) : IRequest;

internal sealed class SetAsPrimaryEmailCommandHandler : IRequestHandler<SetAsPrimaryEmailCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public SetAsPrimaryEmailCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(SetAsPrimaryEmailCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

        account.SetAsPrimaryEmail(request.Email);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
