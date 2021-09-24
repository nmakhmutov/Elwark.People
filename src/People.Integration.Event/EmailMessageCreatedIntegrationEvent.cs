using System;
using People.Kafka;

namespace People.Integration.Event
{
    public sealed record EmailMessageCreatedIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        string Email,
        string Subject,
        string Body
    ) : IKafkaMessage;
}
