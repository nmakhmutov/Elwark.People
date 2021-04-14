using System;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record AccountCreatedIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        long AccountId,
        string Email,
        string Ip,
        string Language
    ) : IKafkaMessage;
}
