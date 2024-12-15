using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountUpdatedIntegrationEvent(long AccountId) : IntegrationEvent;
