using People.Kafka.Integration;

namespace People.Webhooks.IntegrationEvents.Events;

public sealed record AccountUpdatedIntegrationEvent(long AccountId) : IntegrationEvent;
