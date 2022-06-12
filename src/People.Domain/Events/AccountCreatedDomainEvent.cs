using System.Net;
using MediatR;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Domain.Events;

public sealed record AccountCreatedDomainEvent(Account Account, IPAddress IpAddress)
    : INotification;
