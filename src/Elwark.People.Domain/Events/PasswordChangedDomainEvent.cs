using System;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class PasswordChangedDomainEvent : INotification
    {
        public PasswordChangedDomainEvent(Account account) =>
            Account = account ?? throw new ArgumentNullException(nameof(account));

        public Account Account { get; }
    }
}