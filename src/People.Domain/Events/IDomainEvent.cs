namespace People.Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
