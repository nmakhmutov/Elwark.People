using System.Threading;
using System.Threading.Tasks;
using MediatR;
using People.Api.Infrastructure;
using People.Domain.Aggregates.AccountAggregate;
using People.Domain.Exceptions;

namespace People.Api.Application.Commands.CreateRole;

internal sealed record CreateRoleCommand(AccountId Id, string Role) : IRequest;

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand>
{
    private readonly IMediator _mediator;
    private readonly IAccountRepository _repository;

    public CreateRoleCommandHandler(IMediator mediator, IAccountRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    public async Task<Unit> Handle(CreateRoleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            throw new ElwarkException(ElwarkExceptionCodes.AccountNotFound);

        account.AddRole(request.Role);

        await _repository.UpdateAsync(account, ct);
        await _mediator.DispatchDomainEventsAsync(account);

        return Unit.Value;
    }
}
