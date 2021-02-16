using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Kafka
{
    public class KafkaBuilder : IKafkaBuilder
    {
        public IServiceCollection Services { get; }

        public KafkaBuilder(IServiceCollection services) =>
            Services = services;
    }
}