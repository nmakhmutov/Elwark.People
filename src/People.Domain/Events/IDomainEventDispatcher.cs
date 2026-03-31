namespace People.Domain.Events;

public interface IDomainEventDispatcher
{
    ValueTask DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default);
}
