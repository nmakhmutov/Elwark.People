using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.DeleteRole;

internal sealed record DeleteRoleCommand(AccountId Id, string Role) : IRequest;

internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public DeleteRoleCommandHandler(IMediator mediator, IAccountRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteRoleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

        account.DeleteRole(request.Role);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
