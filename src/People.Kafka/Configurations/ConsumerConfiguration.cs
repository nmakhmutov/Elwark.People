using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace People.Kafka.Configurations;

public sealed record ConsumerConfiguration(
    string Topic,
    byte Workers,
    byte RetryCount,
    TimeSpan RetryInterval,
    TopicSpecification? TopicSpecification,
    ConsumerConfig Config
);
