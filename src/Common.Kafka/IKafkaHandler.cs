namespace Common.Kafka;

public interface IKafkaHandler<in T> where T : IKafkaMessage
{
    Task HandleAsync(T message);
}
