using System;
using MediatR;
using People.Domain.AggregateModels.Account;

namespace People.Domain.Events
{
    public sealed class AccountCreatedDomainEvent : INotification
    {
        public AccountCreatedDomainEvent(Account account) =>
            Account = account ?? throw new ArgumentNullException(nameof(account));

        public Account Account { get; }
    }
}