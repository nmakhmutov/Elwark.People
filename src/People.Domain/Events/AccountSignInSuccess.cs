using System.Net;
using MediatR;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Domain.Events
{
    public sealed record AccountSignInSuccess(Account Account, IPAddress IpAddress) : INotification;
}
