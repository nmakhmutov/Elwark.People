using People.Infrastructure.Integration;

namespace People.Infrastructure.Kafka;

public interface IKafkaHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T message);
}
