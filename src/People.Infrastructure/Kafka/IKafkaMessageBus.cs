using System.Threading;
using System.Threading.Tasks;

namespace People.Infrastructure.Kafka
{
    public interface IKafkaMessageBus
    {
        Task PublishAsync<T>(T message, CancellationToken ct = default) where T : IKafkaMessage;
    }
}