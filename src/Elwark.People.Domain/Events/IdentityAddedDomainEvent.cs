using System;
using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class IdentityAddedDomainEvent : INotification
    {
        public IdentityAddedDomainEvent(Account account, Identification identification)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            Identification = identification ?? throw new ArgumentNullException(nameof(identification));
        }

        public Account Account { get; }

        public Identification Identification { get; }
    }
}