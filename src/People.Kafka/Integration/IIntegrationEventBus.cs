namespace People.Kafka.Integration;

public interface IIntegrationEventBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default)
        where T : IIntegrationEvent;

    Task PublishAsync<T>(ICollection<T> messages, CancellationToken ct = default)
        where T : IIntegrationEvent;
}
