using System;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record EmailMessageCreatedIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        string Email,
        string Subject,
        string Body
    ) : IKafkaMessage;
}
