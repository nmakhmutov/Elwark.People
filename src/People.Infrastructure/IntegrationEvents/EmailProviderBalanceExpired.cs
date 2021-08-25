using System;
using People.Domain.Aggregates.EmailProviderAggregate;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record ProviderExpiredIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        EmailProvider.Type Type
    ) : IKafkaMessage;
}
