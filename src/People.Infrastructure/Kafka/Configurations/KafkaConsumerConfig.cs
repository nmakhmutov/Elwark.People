namespace People.Infrastructure.Kafka.Configurations;

public sealed record KafkaConsumerConfig<T>
{
    public string EventName =>
        typeof(T).Name;

    public string Topic { get; set; } = string.Empty;

    public byte Threads { get; set; } = 1;

    public byte RetryCount { get; set; } = 20;

    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);
}