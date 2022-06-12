using Confluent.Kafka;
using Microsoft.Extensions.Options;
using People.Infrastructure.Integration;

namespace People.Infrastructure.Kafka;

internal interface IKafkaProducer : IDisposable
{
    Task ProduceAsync(object message, CancellationToken ct = default);
}

internal interface IKafkaProducer<in T> : IKafkaProducer where T : IIntegrationEvent
{
    Task ProduceAsync(T message, CancellationToken ct = default);
}

internal sealed class KafkaProducer<T> : IKafkaProducer<T> where T : IIntegrationEvent
{
    private readonly IProducer<string, T> _producer;
    private readonly string _topic;

    public KafkaProducer(IProducer<string, T> producer, IOptions<KafkaProducerConfig<T>> options)
    {
        _producer = producer;
        _topic = options.Value.Topic;
    }

    public void Dispose() =>
        _producer.Dispose();

    public Task ProduceAsync(object message, CancellationToken ct) =>
        ProduceAsync((T)message, ct);

    public Task ProduceAsync(T message, CancellationToken ct)
    {
        var kafkaMessage = new Message<string, T>
        {
            Key = message.MessageId.ToString("D"),
            Timestamp = new Timestamp(message.CreatedAt),
            Value = message
        };

        return _producer.ProduceAsync(_topic, kafkaMessage, ct);
    }
}
