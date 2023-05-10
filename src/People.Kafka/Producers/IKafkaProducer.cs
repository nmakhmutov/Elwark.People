using People.Kafka.Integration;

namespace People.Kafka.Producers;

internal interface IKafkaProducer : IDisposable
{
    Task ProduceAsync(object message, CancellationToken ct = default);
}

internal interface IKafkaProducer<in T> : IKafkaProducer where T : IIntegrationEvent
{
    Task ProduceAsync(T message, CancellationToken ct = default);
}
