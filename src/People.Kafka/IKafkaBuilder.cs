using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka;

public interface IKafkaBuilder
{
    IServiceCollection Services { get; }

    internal string Brokers { get; }
}
