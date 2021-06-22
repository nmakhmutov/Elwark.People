using System;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record AccountUpdatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id)
        : IKafkaMessage;
}
