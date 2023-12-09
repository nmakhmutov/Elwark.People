using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using People.Kafka.Configurations;
using People.Kafka.Converters;
using People.Kafka.Integration;

namespace People.Kafka.Producers;

internal sealed class KafkaProducer<T> : IKafkaProducer<T> where T : IIntegrationEvent
{
    private readonly IProducer<Guid, T> _producer;
    private readonly string _topic;

    public KafkaProducer(ProducerConfiguration configuration, ILogger logger)
    {
        _topic = configuration.Topic;

        _producer = new ProducerBuilder<Guid, T>(configuration.Config)
            .SetErrorHandler((_, error) => logger.LogError("Error occured on publishing {E}", error))
            .SetKeySerializer(KafkaKeyConverter.Instance)
            .SetValueSerializer(KafkaValueConverter<T>.Instance)
            .Build();
    }

    public Task ProduceAsync(object message, CancellationToken ct) =>
        ProduceAsync((T)message, ct);

    public Task ProduceAsync(T message, CancellationToken ct)
    {
        Activity.Current ??= new Activity(nameof(KafkaProducer<T>)).Start();

        var kafkaMessage = new Message<Guid, T>
        {
            Key = message.MessageId,
            Value = message,
            Timestamp = new Timestamp(message.CreatedAt),
            Headers = new Headers
            {
                { nameof(Activity.TraceId), Encoding.UTF8.GetBytes(Activity.Current.TraceId.ToHexString()) },
                { nameof(Activity.SpanId), Encoding.UTF8.GetBytes(Activity.Current.SpanId.ToHexString()) }
            }
        };

        return _producer.ProduceAsync(_topic, kafkaMessage, ct);
    }

    public void Dispose() =>
        _producer.Dispose();
}
