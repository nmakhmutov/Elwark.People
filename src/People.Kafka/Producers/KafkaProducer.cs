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
    private readonly byte[] _clientId;
    private readonly IProducer<Guid, T> _producer;
    private readonly string _topic;

    public KafkaProducer(ProducerConfiguration configuration, ILogger logger)
    {
        _topic = configuration.Topic;
        _clientId = Encoding.UTF8.GetBytes(configuration.Config.ClientId);

        _producer = new ProducerBuilder<Guid, T>(configuration.Config)
            .SetLogHandler((_, message) =>
            {
                var level = (LogLevel)message.LevelAs(LogLevelType.MicrosoftExtensionsLogging);

                logger.Log(level, $"{message.Message}. {{@Message}}", message);
            })
            .SetErrorHandler((_, error) => logger.PublisherException(error.Reason, error))
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
            Headers =
            [
                new Header(nameof(Activity.TraceId), Convert.FromHexString(Activity.Current.TraceId.ToHexString())),
                new Header(nameof(Activity.SpanId), Convert.FromHexString(Activity.Current.SpanId.ToHexString())),
                new Header("ClientId", _clientId)
            ]
        };

        return _producer.ProduceAsync(_topic, kafkaMessage, ct);
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}
