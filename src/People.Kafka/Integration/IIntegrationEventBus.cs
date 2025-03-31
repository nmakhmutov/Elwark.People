namespace People.Kafka.Integration;

public interface IIntegrationEventBus
{
    ValueTask PublishAsync<T>(T message, CancellationToken ct = default)
        where T : IIntegrationEvent;

    ValueTask PublishAsync<T>(ICollection<T> messages, CancellationToken ct = default)
        where T : IIntegrationEvent;
}
