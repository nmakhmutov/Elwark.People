using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using People.Kafka.Configurations;
using People.Kafka.Converters;
using People.Kafka.Integration;

namespace People.Kafka.Producers;

internal sealed class KafkaProducer<T> : IKafkaProducer<T> where T : IIntegrationEvent
{
    private readonly string _clientId;
    private readonly IProducer<string, T> _producer;
    private readonly string _topic;

    public KafkaProducer(ProducerConfiguration configuration, ILogger logger)
    {
        _topic = configuration.Topic;
        _clientId = configuration.Config.ClientId;

        _producer = new ProducerBuilder<string, T>(configuration.Config)
            .SetLogHandler((_, message) =>
            {
                var level = (LogLevel)message.LevelAs(LogLevelType.MicrosoftExtensionsLogging);

                logger.Log(level, "Producer exception {Name} with message {Message}", message.Name, message.Message);
            })
            .SetErrorHandler((_, error) => logger.PublisherException(error.Reason, error))
            .SetKeySerializer(KafkaKeyConverter.Instance)
            .SetValueSerializer(KafkaValueConverter<T>.Instance)
            .Build();
    }

    public ValueTask ProduceAsync(object message, CancellationToken ct) =>
        ProduceAsync((T)message, ct);

    public ValueTask ProduceAsync(T message, CancellationToken ct)
    {
        var topicKey = message switch
        {
            IKafkaMessage msg => msg.GetTopicKey(),
            var x => x.MessageId.ToString("N")
        };

        var kafkaMessage = new Message<string, T>
        {
            Key = topicKey,
            Value = message,
            Timestamp = new Timestamp(message.CreatedAt),
            Headers =
            [
                new Header("ClientId", Encoding.UTF8.GetBytes(_clientId))
            ]
        };

        _producer.Produce(_topic, kafkaMessage);

        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}
