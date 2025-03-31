using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace People.Kafka.Configurations;

public sealed class ConsumerConfigurationBuilder
{
    private const byte RetryCount = 8;
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(15);
    private string? _groupId;
    private string? _servers;
    private string? _topic;
    private TopicSpecification? _topicSpecification;
    private byte _workers = 1;

    public ConsumerConfigurationBuilder WithTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrEmpty(topic);

        _topic = topic;
        return this;
    }

    public ConsumerConfigurationBuilder WithGroupId(string groupId)
    {
        ArgumentException.ThrowIfNullOrEmpty(groupId);

        _groupId = groupId;
        return this;
    }

    public ConsumerConfigurationBuilder WithWorkers(byte workers)
    {
        ArgumentOutOfRangeException.ThrowIfZero(workers);

        _workers = workers;
        return this;
    }

    public ConsumerConfigurationBuilder CreateTopicIfNotExists() =>
        CreateTopicIfNotExists(8, TimeSpan.FromDays(4));

    public ConsumerConfigurationBuilder CreateTopicIfNotExists(int partitions, short replicas = 1) =>
        CreateTopicIfNotExists(partitions, TimeSpan.FromDays(4), replicas);

    public ConsumerConfigurationBuilder CreateTopicIfNotExists(int partitions, TimeSpan retention, short replicas = 1)
    {
        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka topic not specified", true));

        _topicSpecification = new TopicSpecification
        {
            Name = _topic,
            NumPartitions = partitions,
            ReplicationFactor = replicas,
            Configs = new Dictionary<string, string>
            {
                ["cleanup.policy"] = "delete",
                ["retention.ms"] = retention.TotalMilliseconds.ToString("0000")
            }
        };

        return this;
    }

    internal ConsumerConfigurationBuilder WithServers(string servers)
    {
        _servers = servers;

        return this;
    }

    internal ConsumerConfiguration Build()
    {
        if (string.IsNullOrEmpty(_servers))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka servers not specified", true));

        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka topic not specified", true));

        if (string.IsNullOrEmpty(_groupId))
            throw new KafkaException(new Error(ErrorCode.InvalidConfig, "Kafka group id is not specified", true));

        var config = new ConsumerConfig
        {
            BootstrapServers = _servers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnablePartitionEof = false,
            EnableAutoOffsetStore = false
        };

        return new ConsumerConfiguration(_topic, _workers, RetryCount, _retryInterval, _topicSpecification, config);
    }
}
