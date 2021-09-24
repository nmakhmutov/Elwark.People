using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace People.Kafka
{
    internal sealed class KafkaMessageBus : IKafkaMessageBus
    {
        private readonly IServiceScopeFactory _factory;
        private readonly ILogger<KafkaMessageBus> _logger;

        public KafkaMessageBus(IServiceScopeFactory factory, ILogger<KafkaMessageBus> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T message, CancellationToken ct) where T : IKafkaMessage
        {
            using var scope = _factory.CreateAsyncScope();
            var producer = scope.ServiceProvider.GetService<KafkaProducer<T>>();

            if (producer is null)
                throw new Exception($"For message type '{typeof(T)}' producer not found");

            await producer.ProduceAsync(message, ct);
            _logger.LogInformation("Kafka message sent. {M}", message);
        }
    }
}
