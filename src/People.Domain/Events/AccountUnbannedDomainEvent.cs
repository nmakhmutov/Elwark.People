using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Domain.Events;

public sealed record AccountUnbannedDomainEvent(Account Account) : INotification;
