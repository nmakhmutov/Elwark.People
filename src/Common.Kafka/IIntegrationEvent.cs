namespace Common.Kafka;

public interface IIntegrationEvent
{
    public Guid MessageId { get; }

    public DateTime CreatedAt { get; }
}
