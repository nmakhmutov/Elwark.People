using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka;

public interface IKafkaBuilder
{
    public IServiceCollection Services { get; }

    internal string Brokers { get; }
}
