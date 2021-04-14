using System;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record AccountInfoReceivedIntegrationEvent(
        Guid MessageId,
        DateTime CreatedAt,
        long AccountId,
        string Ip,
        string? CountryCode,
        string? City,
        string? Timezone,
        string? FirstName,
        string? LastName,
        string? AboutMe,
        Uri? Image
    ) : IKafkaMessage;
}
