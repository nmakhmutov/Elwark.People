using System;
using People.Kafka;

namespace People.Integration.Event
{
    public sealed record AccountUpdatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id)
        : IKafkaMessage;
}
