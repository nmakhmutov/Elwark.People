using Microsoft.Extensions.DependencyInjection;

namespace Common.Kafka;

public interface IKafkaBuilder
{
    public IServiceCollection Services { get; }
}
