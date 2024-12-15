using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountDeletedIntegrationEvent(long AccountId) : IntegrationEvent;
