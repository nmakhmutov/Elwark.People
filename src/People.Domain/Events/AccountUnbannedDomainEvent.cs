using MediatR;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Domain.Events;

public sealed record AccountUnbannedDomainEvent(Account Account) : INotification;