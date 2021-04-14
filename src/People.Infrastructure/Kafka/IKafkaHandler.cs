using System.Threading.Tasks;

namespace People.Infrastructure.Kafka
{
    public interface IKafkaHandler<in T> where T : IKafkaMessage
    {
        Task HandleAsync(T message);
    }
}
