namespace Common.Kafka;

public interface IKafkaMessage
{
    public Guid MessageId { get; }

    public DateTime CreatedAt { get; }
}
