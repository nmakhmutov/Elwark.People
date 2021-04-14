using System;

namespace People.Infrastructure.Kafka
{
    public interface IKafkaMessage
    {
        public Guid MessageId { get; init; }
        
        public DateTime CreatedAt { get; init; }
    }
}
