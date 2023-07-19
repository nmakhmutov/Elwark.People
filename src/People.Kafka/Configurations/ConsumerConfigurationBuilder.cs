using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace People.Kafka.Configurations;

public sealed class ConsumerConfigurationBuilder
{
    private string? _groupId;
    private byte _retryCount = 5;
    private TimeSpan _retryInterval = TimeSpan.FromSeconds(12);
    private string? _topic;
    private TopicSpecification? _topicSpecification;
    private byte _workers = 1;

    public ConsumerConfigurationBuilder WithTopic(string topic)
    {
        _topic = topic;
        return this;
    }

    public ConsumerConfigurationBuilder WithGroupId(string groupId)
    {
        _groupId = groupId;
        return this;
    }

    public ConsumerConfigurationBuilder WithWorkers(byte workers)
    {
        if (workers <= 0)
            throw new ArgumentOutOfRangeException(nameof(workers));

        _workers = workers;
        return this;
    }

    public ConsumerConfigurationBuilder WithRetryCount(byte count)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        _retryCount = count;
        return this;
    }

    public ConsumerConfigurationBuilder WithRetryInterval(TimeSpan interval)
    {
        _retryInterval = interval;
        return this;
    }

    public ConsumerConfigurationBuilder CreateTopicIfNotExists(int numPartitions, short replicationFactor = 1)
    {
        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka topic not specified"));

        _topicSpecification = new TopicSpecification
        {
            Name = _topic,
            NumPartitions = numPartitions,
            ReplicationFactor = replicationFactor,
            Configs = new Dictionary<string, string>
            {
                ["cleanup.policy"] = "delete",
                ["retention.ms"] = TimeSpan.FromDays(14).TotalMilliseconds.ToString("0000")
            }
        };

        return this;
    }

    internal ConsumerConfiguration Build(string brokers)
    {
        if (string.IsNullOrEmpty(_topic))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka topic not specified"));

        if (string.IsNullOrEmpty(_groupId))
            throw new KafkaException(ErrorCode.InvalidConfig, new Exception("Kafka group id is not specified"));

        return new ConsumerConfiguration(
            _topic,
            _workers,
            _retryCount,
            _retryInterval,
            _topicSpecification,
            new ConsumerConfig
            {
                BootstrapServers = brokers,
                GroupId = _groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnablePartitionEof = false,
                EnableAutoOffsetStore = false
            }
        );
    }
}
