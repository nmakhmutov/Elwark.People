using System.Net;
using People.Domain.Entities;
using People.Domain.Events;

namespace People.Domain.DomainEvents;

public sealed record AccountCreatedDomainEvent(Account Account, IPAddress IpAddress, DateTime OccurredAt)
    : IDomainEvent;
