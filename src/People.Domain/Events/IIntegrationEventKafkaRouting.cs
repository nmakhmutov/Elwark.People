namespace People.Domain.Events;

/// <summary>Supplies the Kafka message key for <see cref="IIntegrationEvent"/> types published via People.Kafka.</summary>
public interface IIntegrationEventKafkaRouting
{
    string KafkaPartitionKey { get; }
}
