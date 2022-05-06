using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.ConfuteConnection;

internal sealed record ConfuteConnectionCommand(AccountId Id, Identity Identity) : IRequest;

internal sealed class ConfuteConnectionCommandHandler : IRequestHandler<ConfuteConnectionCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public ConfuteConnectionCommandHandler(IAccountRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(ConfuteConnectionCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct)
                      ?? throw new PeopleException(ExceptionCodes.AccountNotFound);

        account.ConfuteConnection(request.Identity);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
