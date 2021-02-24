using System;
using System.Net;
using MediatR;
using People.Domain.AggregateModels.Account;

namespace People.Domain.Events
{
    public sealed class AccountCreatedDomainEvent : INotification
    {
        public AccountCreatedDomainEvent(Account account, IPAddress ipAddress)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            IpAddress = ipAddress;
        }

        public Account Account { get; }
        
        public IPAddress IpAddress { get; }
    }
}