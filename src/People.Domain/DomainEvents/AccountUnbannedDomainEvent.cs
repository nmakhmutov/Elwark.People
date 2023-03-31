using MediatR;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record AccountUnbannedDomainEvent(Account Account) : INotification;
