using System.Net;
using MediatR;

namespace People.Account.Domain.Events
{
    public sealed record AccountSignInSuccess(Aggregates.AccountAggregate.Account Account, IPAddress IpAddress) : INotification;
}
