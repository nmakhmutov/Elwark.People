using People.Domain.Entities;
using People.Domain.Events;

namespace People.Domain.DomainEvents;

public sealed record AccountBannedDomainEvent(
    AccountId Id,
    string Reason,
    DateTime ExpiredAt,
    DateTime OccurredAt
) : IDomainEvent;
