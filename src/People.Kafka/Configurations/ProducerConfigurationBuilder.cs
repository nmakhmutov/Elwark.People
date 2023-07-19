using Confluent.Kafka;

namespace People.Kafka.Configurations;

public sealed class ProducerConfigurationBuilder
{
    private Acks _acks = Acks.Leader;
    private string? _topic;

    public ProducerConfigurationBuilder WithTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    public ProducerConfigurationBuilder WithAcks(Acks acks)
    {
        _acks = acks;
        return this;
    }

    internal ProducerConfiguration Build(string brokers)
    {
        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka topic not specified"));

        return new ProducerConfiguration(
            _topic,
            new ProducerConfig
            {
                BootstrapServers = brokers,
                Acks = _acks,
                EnableDeliveryReports = true
            }
        );
    }
}
