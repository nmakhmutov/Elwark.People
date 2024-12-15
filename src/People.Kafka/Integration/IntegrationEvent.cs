namespace People.Kafka.Integration;

public abstract record IntegrationEvent(Guid MessageId, DateTime CreatedAt) : IIntegrationEvent
{
    private IntegrationEvent(DateTimeOffset date)
        : this(Guid.CreateVersion7(date), date.UtcDateTime)
    {
    }

    protected IntegrationEvent()
        : this(DateTimeOffset.UtcNow)
    {
    }
}
