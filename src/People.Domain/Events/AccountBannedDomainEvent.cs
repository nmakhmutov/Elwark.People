using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Domain.Events;

public sealed record AccountBannedDomainEvent(Account Account, string Reason, DateTime ExpiredAt) : INotification;
