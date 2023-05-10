namespace People.Kafka.Integration;

public interface IIntegrationEvent
{
    public Guid MessageId { get; }

    public DateTime CreatedAt { get; }
}
