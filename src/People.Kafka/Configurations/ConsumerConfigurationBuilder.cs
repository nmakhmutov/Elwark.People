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
        var topic = _topic ?? throw new Exception("Topic not specified");

        _topicSpecification = new TopicSpecification
        {
            Name = topic,
            NumPartitions = numPartitions,
            ReplicationFactor = replicationFactor,
            Configs = new Dictionary<string, string>
            {
                ["cleanup.policy"] = "delete",
                ["retention.ms"] = TimeSpan.FromDays(7).TotalMilliseconds.ToString("0000")
            }
        };

        return this;
    }

    internal ConsumerConfiguration Build(string brokers) =>
        new(
            _topic ?? throw new Exception("Topic is not specified"),
            _workers,
            _retryCount,
            _retryInterval,
            _topicSpecification,
            new ConsumerConfig
            {
                BootstrapServers = brokers,
                GroupId = _groupId ?? throw new Exception("Group id is not specified"),
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnablePartitionEof = false,
                EnableAutoOffsetStore = false
            }
        );
}
