namespace People.Kafka.Integration;

public interface IIntegrationEventHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T message);
}
