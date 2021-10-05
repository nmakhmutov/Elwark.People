using System.Net;
using MediatR;

namespace People.Domain.Events;

public sealed record AccountCreatedDomainEvent(Aggregates.AccountAggregate.Account Account, IPAddress IpAddress) 
    : INotification;
