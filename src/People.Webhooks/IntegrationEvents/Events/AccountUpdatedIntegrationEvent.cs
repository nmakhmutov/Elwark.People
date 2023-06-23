using People.Kafka.Integration;

namespace People.Webhooks.IntegrationEvents.Events;

public sealed record AccountUpdatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId)
    : IIntegrationEvent;
