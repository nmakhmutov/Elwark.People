using MediatR;
using People.Domain.Entities;
using People.Domain.Exceptions;
using People.Domain.Repositories;

namespace People.Api.Application.Commands.DeleteGoogle;

internal sealed record DeleteGoogleCommand(AccountId Id, string Identity) : IRequest;

internal sealed class DeleteGoogleCommandHandler : IRequestHandler<DeleteGoogleCommand>
{
    private readonly IAccountRepository _repository;

    public DeleteGoogleCommandHandler(IAccountRepository repository) =>
        _repository = repository;

    public async Task Handle(DeleteGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.DeleteGoogle(request.Identity);

        _repository.Update(account);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);
    }
}
