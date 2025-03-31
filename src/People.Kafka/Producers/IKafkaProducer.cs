using People.Kafka.Integration;

namespace People.Kafka.Producers;

internal interface IKafkaProducer : IDisposable
{
    ValueTask ProduceAsync(object message, CancellationToken ct = default);
}

internal interface IKafkaProducer<in T> : IKafkaProducer where T : IIntegrationEvent
{
    ValueTask ProduceAsync(T message, CancellationToken ct = default);
}
