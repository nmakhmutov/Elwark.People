namespace People.Kafka;

public interface IKafkaMessage
{
    string GetTopicKey();
}
