using Mediator;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record AccountUpdatedDomainEvent(AccountId Id) : INotification;
