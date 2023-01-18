namespace People.Infrastructure.Kafka.Configurations;

public sealed record KafkaProducerConfig<T>
{
    public string EventName =>
        typeof(T).Name;

    public string Topic { get; set; } = string.Empty;
}
