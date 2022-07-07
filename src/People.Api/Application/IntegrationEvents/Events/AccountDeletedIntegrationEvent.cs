using People.Infrastructure.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountDeletedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) 
    : IIntegrationEvent;
