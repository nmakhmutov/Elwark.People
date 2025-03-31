using Confluent.Kafka;

namespace People.Kafka.Configurations;

public sealed class ProducerConfigurationBuilder
{
    private string? _clientId;
    private string? _servers;
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

    internal ProducerConfigurationBuilder WithServers(string servers)
    {
        _servers = servers;
        return this;
    }

    internal ProducerConfiguration Build()
    {
        if (string.IsNullOrEmpty(_servers))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka servers not specified", true));

        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka topic not specified", true));

        if (string.IsNullOrEmpty(_clientId))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka client id is not specified", true));

        var config = new ProducerConfig
        {
            BootstrapServers = _servers,
            ClientId = _clientId,
            Acks = Acks.Leader,
            EnableDeliveryReports = true,
            AllowAutoCreateTopics = false,
            LingerMs = 10
        };

        return new ProducerConfiguration(_topic, config);
    }
}
