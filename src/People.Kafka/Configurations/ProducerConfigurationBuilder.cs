using Confluent.Kafka;

namespace People.Kafka.Configurations;

public sealed class ProducerConfigurationBuilder
{
    private string? _clientId;
    private string? _topic;

    public ProducerConfigurationBuilder WithTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    public ProducerConfigurationBuilder WithClientId(string clientId)
    {
        _clientId = clientId;
        return this;
    }

    internal ProducerConfiguration Build(string brokers)
    {
        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka topic not specified"));

        if (string.IsNullOrEmpty(_clientId))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka client id not specified"));

        var config = new ProducerConfig
        {
            BootstrapServers = brokers,
            ClientId = _clientId,
            Acks = Acks.Leader,
            EnableDeliveryReports = true,
            AllowAutoCreateTopics = false
        };

        return new ProducerConfiguration(_topic, config);
    }
}
