using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka;

public interface IKafkaBuilder
{
    IServiceCollection Services { get; }

    string Brokers { get; }
}
