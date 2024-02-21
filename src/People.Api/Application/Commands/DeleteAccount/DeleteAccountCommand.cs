using MediatR;
using People.Domain.Entities;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.DeleteAccount;

internal sealed record DeleteAccountCommand(AccountId Id) : IRequest;

internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IAccountRepository _repository;

    public DeleteAccountCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct);
        if (account is null)
            return;

        account.Delete();

        _repository.Delete(account);

        await _repository.UnitOfWork.SaveEntitiesAsync(ct);
    }
}
