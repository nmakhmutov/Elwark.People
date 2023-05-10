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

    internal ProducerConfiguration Build(string brokers) =>
        new(
            _topic ?? throw new ArgumentNullException(),
            new ProducerConfig
            {
                BootstrapServers = brokers,
                Acks = _acks,
                EnableDeliveryReports = true
            }
        );
}
