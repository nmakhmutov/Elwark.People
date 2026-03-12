using Mediator;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record AccountDeletedDomainEvent(AccountId Id) : INotification;
