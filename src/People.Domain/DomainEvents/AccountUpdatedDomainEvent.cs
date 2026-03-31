using People.Domain.Entities;
using People.Domain.Events;

namespace People.Domain.DomainEvents;

public sealed record AccountUpdatedDomainEvent(AccountId Id, DateTime OccurredAt) : IDomainEvent;
