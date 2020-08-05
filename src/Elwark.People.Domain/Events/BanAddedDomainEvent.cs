using System;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class BanAddedDomainEvent : INotification
    {
        public BanAddedDomainEvent(Account account, Ban ban)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Ban = ban ?? throw new ArgumentNullException(nameof(ban));
        }

        public Account Account { get; }

        public Ban Ban { get; }
    }
}