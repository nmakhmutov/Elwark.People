using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka
{
    public sealed class KafkaBuilder : IKafkaBuilder
    {
        public KafkaBuilder(IServiceCollection services) =>
            Services = services;

        public IServiceCollection Services { get; }
    }
}
