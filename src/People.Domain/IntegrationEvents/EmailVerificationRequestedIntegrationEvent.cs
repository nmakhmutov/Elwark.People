using People.Domain.Events;
using People.Domain.ValueObjects;

namespace People.Domain.IntegrationEvents;

public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid Id,
    long AccountId,
    string Email,
    Language Language,
    DateTime OccurredAt
) : IIntegrationEvent;
