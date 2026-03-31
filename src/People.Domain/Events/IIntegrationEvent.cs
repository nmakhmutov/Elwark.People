namespace People.Domain.Events;

public interface IIntegrationEvent
{
    Guid Id { get; }

    DateTime OccurredAt { get; }
}
