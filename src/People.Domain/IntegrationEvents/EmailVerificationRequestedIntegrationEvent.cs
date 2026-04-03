using People.Domain.Events;

namespace People.Domain.IntegrationEvents;

public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid Id,
    long AccountId,
    Guid ConfirmationId,
    string Email,
    string Language,
    DateTime OccurredAt
) : IIntegrationEvent;
