using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

namespace People.Api.Application.Commands.DeleteGoogle;

internal sealed record DeleteGoogleCommand(long Id, string Identity) : IRequest;

internal sealed class DeleteGoogleCommandHandler : IRequestHandler<DeleteGoogleCommand>
{
    private readonly IAccountRepository _repository;
    private readonly ITimeProvider _time;
    
    public DeleteGoogleCommandHandler(IAccountRepository repository, ITimeProvider time)
    {
        _repository = repository;
        _time = time;
    }

    public async Task<Unit> Handle(DeleteGoogleCommand request, CancellationToken ct)
    {
        var account = await _repository.GetAsync(request.Id, ct) ?? throw AccountException.NotFound(request.Id);
        account.DeleteGoogle(request.Identity, _time);

        _repository.Update(account);
        await _repository.UnitOfWork.SaveEntitiesAsync(ct);
        
        return Unit.Value;
    }
}