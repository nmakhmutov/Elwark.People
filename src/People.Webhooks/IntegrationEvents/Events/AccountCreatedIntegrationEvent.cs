using People.Kafka.Integration;

namespace People.Webhooks.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(long AccountId, string Ip) : IntegrationEvent;
