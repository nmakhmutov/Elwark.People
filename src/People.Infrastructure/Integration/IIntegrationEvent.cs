namespace People.Infrastructure.Integration;

public interface IIntegrationEvent
{
    public Guid MessageId { get; }

    public DateTime CreatedAt { get; }
}
