namespace People.Domain.Events;

public interface IDomainEventHandler
{
    bool CanHandle(IDomainEvent domainEvent);

    ValueTask HandleAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
