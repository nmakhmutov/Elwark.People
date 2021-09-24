using Microsoft.Extensions.DependencyInjection;

namespace People.Kafka
{
    public interface IKafkaBuilder
    {
        public IServiceCollection Services { get; }
    }
}
