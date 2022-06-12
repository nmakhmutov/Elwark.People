using People.Infrastructure.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id, string Ip)
    : IIntegrationEvent;
