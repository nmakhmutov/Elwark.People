using People.Domain.Events;

namespace People.Domain.IntegrationEvents;

public sealed record AccountUpdatedIntegrationEvent(
    Guid Id,
    long AccountId,
    DateTime OccurredAt
) : IIntegrationEvent;
