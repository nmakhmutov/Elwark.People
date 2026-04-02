using Mediator;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Application.Commands.DeleteMicrosoft;

public sealed record DeleteMicrosoftCommand(AccountId Id, string Identity) : ICommand;

public sealed class DeleteMicrosoftCommandHandler : ICommandHandler<DeleteMicrosoftCommand>
{
    private readonly IAccountRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeleteMicrosoftCommandHandler(IAccountRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async ValueTask<Unit> Handle(DeleteMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.DeleteMicrosoft(request.Identity, _timeProvider);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
