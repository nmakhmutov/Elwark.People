using System;
using People.Domain.Aggregates.EmailProvider;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record ProviderExpiredIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        EmailProviderType Type
    ) : IKafkaMessage;
}
