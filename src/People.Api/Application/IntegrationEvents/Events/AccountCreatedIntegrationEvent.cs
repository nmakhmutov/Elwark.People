using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(long AccountId, string Ip) : IntegrationEvent;
