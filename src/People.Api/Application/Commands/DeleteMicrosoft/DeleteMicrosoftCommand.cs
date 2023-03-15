using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

namespace People.Api.Application.Commands.DeleteMicrosoft;

internal sealed record DeleteMicrosoftCommand(long Id, string Identity) : IRequest;

internal sealed class DeleteMicrosoftCommandHandler : IRequestHandler<DeleteMicrosoftCommand>
{
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;

    public DeleteMicrosoftCommandHandler(IAccountRepository repository, ITimeProvider time)
    {
        _repository = repository;
        _time = time;
    }

    public async Task Handle(DeleteMicrosoftCommand request, CancellationToken ct)
    {
        var account = await _repository
            .GetAsync(request.Id, ct)
            .ConfigureAwait(false) ?? throw AccountException.NotFound(request.Id);

        account.DeleteMicrosoft(request.Identity, _time);

        _repository.Update(account);
        await _repository.UnitOfWork
            .SaveEntitiesAsync(ct)
            .ConfigureAwait(false);
    }
}
