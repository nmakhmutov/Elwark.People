using Mediator;
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

    public async ValueTask<Unit> Handle(DeleteGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);

        account.DeleteGoogle(request.Identity);

        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct);

        return Unit.Value;
    }
}
