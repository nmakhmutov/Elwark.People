using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Kafka
{
    public class KafkaBuilder : IKafkaBuilder
    {
        public KafkaBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
