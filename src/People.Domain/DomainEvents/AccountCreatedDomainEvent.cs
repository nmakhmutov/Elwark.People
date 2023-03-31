using System.Net;
using MediatR;
using People.Domain.Entities;

namespace People.Domain.DomainEvents;

public sealed record AccountCreatedDomainEvent(Account Account, IPAddress IpAddress)
    : INotification;
