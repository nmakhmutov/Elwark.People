using System;

namespace People.Infrastructure.Kafka
{
    public sealed record KafkaProducerConfig<T>
    {
        public Type MessageType => typeof(T);

        public string Topic { get; set; } = string.Empty;
    }
}
