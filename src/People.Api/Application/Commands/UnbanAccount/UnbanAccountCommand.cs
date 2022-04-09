using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.UnbanAccount;

internal sealed record UnbanAccountCommand(AccountId Id) : IRequest;

internal sealed class UnbanAccountCommandHandler : IRequestHandler<UnbanAccountCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public UnbanAccountCommandHandler(IMediator mediator, IAccountRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    public async Task<Unit> Handle(UnbanAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.Unban();

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
