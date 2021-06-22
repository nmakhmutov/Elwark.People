using System.Net;
using MediatR;
using People.Domain.Aggregates.Account;

namespace People.Domain.Events
{
    public sealed record AccountCreatedDomainEvent(Account Account, IPAddress IpAddress) : INotification;
}
