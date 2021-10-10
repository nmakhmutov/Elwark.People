using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Kafka;
using Integration.Event;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;
using People.Infrastructure;

namespace People.Api.Application.Commands.DeleteAccount;

internal sealed record DeleteAccountCommand(AccountId Id) : IRequest;

internal sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IKafkaMessageBus _bus;
    private readonly PeopleDbContext _dbContext;
    private readonly IAccountRepository _repository;

    public DeleteAccountCommandHandler(IKafkaMessageBus bus, IAccountRepository repository, PeopleDbContext dbContext)
    {
        _bus = bus;
        _repository = repository;
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken ct)
    {
        await _dbContext.StartSessionAsync(ct);

        try
        {
            var evt = new AccountDeletedIntegrationEvent(Guid.NewGuid(), DateTime.UtcNow, (long)request.Id);
            await _repository.DeleteAsync(request.Id, ct);
            await _bus.PublishAsync(evt, ct);

            await _dbContext.CommitAsync(ct);
        }
        catch
        {
            await _dbContext.RollbackAsync(ct);
            throw;
        }

        return Unit.Value;
    }
}
