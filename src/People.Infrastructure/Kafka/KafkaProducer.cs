using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace People.Infrastructure.Kafka
{
    internal sealed class KafkaProducer<T> : IDisposable where T : IKafkaMessage
    {
        private readonly IProducer<Null, T> _producer;
        private readonly string _topic;

        public KafkaProducer(IProducer<Null, T> producer, IOptions<KafkaProducerConfig<T>> options)
        {
            _producer = producer;
            _topic = options.Value.Topic;
        }

        public void Dispose() =>
            _producer.Dispose();

        public Task ProduceAsync(T value, CancellationToken ct) =>
            _producer.ProduceAsync(_topic, new Message<Null, T> {Value = value}, ct);
    }
}