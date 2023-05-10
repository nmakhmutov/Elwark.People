using Confluent.Kafka;

namespace People.Kafka.Configurations;

public sealed record ProducerConfiguration(string Topic, ProducerConfig Config);
