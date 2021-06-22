using System.Net;
using MediatR;
using People.Domain.Aggregates.Account;

namespace People.Domain.Events
{
    public sealed record AccountSignInSuccess(Account Account, IPAddress IpAddress) : INotification;
}
