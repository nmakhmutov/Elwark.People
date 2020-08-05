using Elwark.People.Abstractions;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class IdentityConfirmedDomainEvent : INotification
    {
        public IdentityConfirmedDomainEvent(Account account, Identification identification)
        {
            Account = account;
            Identification = identification;
        }

        public Account Account { get; }

        public Identification Identification { get; }
    }
}