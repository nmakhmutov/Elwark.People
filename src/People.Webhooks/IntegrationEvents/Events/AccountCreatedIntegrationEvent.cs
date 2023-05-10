using People.Kafka.Integration;

namespace People.Webhooks.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId, string Ip)
    : IIntegrationEvent;
