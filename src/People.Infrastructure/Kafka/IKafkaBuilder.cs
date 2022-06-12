using Microsoft.Extensions.DependencyInjection;

namespace People.Infrastructure.Kafka;

public interface IKafkaBuilder
{
    public IServiceCollection Services { get; }
}
