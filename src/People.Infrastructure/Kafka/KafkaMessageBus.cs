using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Kafka
{
    internal sealed class KafkaMessageBus : IKafkaMessageBus
    {
        private readonly IServiceScopeFactory _factory;

        public KafkaMessageBus(IServiceScopeFactory factory) =>
            _factory = factory;

        public async Task PublishAsync<T>(T message, CancellationToken ct) where T : IKafkaMessage
        {
            using var scope = _factory.CreateScope();
            var producer = scope.ServiceProvider.GetService<KafkaProducer<T>>();
            
            if(producer is null)
                throw new NotImplementedException($"For message type {typeof(T)} producer not found");
            
            await producer.ProduceAsync(message, ct);
        }
    }
}