namespace Common.Kafka;

public interface IKafkaHandler<in T> where T : IIntegrationEvent
{
    Task HandleAsync(T message);
}
