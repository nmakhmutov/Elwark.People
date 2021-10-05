namespace Common.Kafka;

public interface IKafkaMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : IKafkaMessage;
}
