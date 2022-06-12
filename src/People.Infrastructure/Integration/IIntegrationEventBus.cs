namespace People.Infrastructure.Integration;

public interface IIntegrationEventBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : IIntegrationEvent;

    Task PublishAsync<T>(IEnumerable<T> messages, CancellationToken ct = default) where T : IIntegrationEvent;
}
