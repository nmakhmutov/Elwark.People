using People.Domain.Events;

namespace People.Domain.IntegrationEvents;

public sealed record AccountCreatedIntegrationEvent(
    Guid Id,
    long AccountId,
    string IpAddress,
    DateTime OccurredAt
) : IIntegrationEvent;
