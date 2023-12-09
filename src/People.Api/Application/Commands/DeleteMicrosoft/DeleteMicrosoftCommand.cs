using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.DeleteMicrosoft;

internal sealed record DeleteMicrosoftCommand(AccountId Id, string Identity) : IRequest;

internal sealed class DeleteMicrosoftCommandHandler : IRequestHandler<DeleteMicrosoftCommand>
{
    private readonly IAccountRepository _repository;

    public DeleteMicrosoftCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task Handle(DeleteMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.DeleteMicrosoft(request.Identity);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);
    }
}
