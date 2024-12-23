using People.Kafka.Integration;

namespace People.Webhooks.IntegrationEvents.Events;

public sealed record AccountDeletedIntegrationEvent(long AccountId) : IntegrationEvent;
