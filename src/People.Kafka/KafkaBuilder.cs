using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka;

internal sealed class KafkaBuilder : IKafkaBuilder
{
    public KafkaBuilder(IServiceCollection services, string brokers)
    {
        Services = services;
        Brokers = brokers;
    }

    public IServiceCollection Services { get; }

    public string Brokers { get; }
}
