using System.Threading.Tasks;

namespace People.Kafka
{
    public interface IKafkaHandler<in T> where T : IKafkaMessage
    {
        Task HandleAsync(T message);
    }
}
