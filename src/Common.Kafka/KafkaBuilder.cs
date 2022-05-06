using Microsoft.Extensions.DependencyInjection;

namespace Common.Kafka;

internal sealed class KafkaBuilder : IKafkaBuilder
{
    public KafkaBuilder(IServiceCollection services) =>
        Services = services;

    public IServiceCollection Services { get; }
}
