using People.Infrastructure.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountUpdatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) 
    : IIntegrationEvent;