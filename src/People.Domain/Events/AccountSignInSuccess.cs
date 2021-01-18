using System.Net;
using MediatR;
using People.Domain.AggregateModels.Account;

namespace People.Domain.Events
{
    public sealed class AccountSignInSuccess : INotification
    {
        public AccountSignInSuccess(Account account, IPAddress ipAddress)
        {
            Account = account;
            IpAddress = ipAddress;
        }

        public Account Account { get; }
        
        public IPAddress IpAddress { get; }
    }
}