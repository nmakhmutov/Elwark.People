using People.Domain.Events;

namespace People.Domain.IntegrationEvents;

public sealed record AccountDeletedIntegrationEvent(
    Guid Id,
    long AccountId,
    DateTime OccurredAt
) : IIntegrationEvent;
