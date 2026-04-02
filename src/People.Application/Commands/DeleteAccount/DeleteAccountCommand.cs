using Mediator;
using People.Domain.Entities;
using People.Domain.Repositories;

namespace People.Application.Commands.DeleteAccount;

public sealed record DeleteAccountCommand(AccountId Id) : ICommand;

public sealed class DeleteAccountCommandHandler : ICommandHandler<DeleteAccountCommand>
{
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeleteAccountCommandHandler(IAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Unit> Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            return Unit.Value;

        account.Delete(_timeProvider);

        _repository.Delete(account);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
