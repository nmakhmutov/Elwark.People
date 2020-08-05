using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class NameChangedDomainEvent : INotification
    {
        public NameChangedDomainEvent(Account account, Name name)
        {
            Account = account;
            Name = name;
        }

        public Account Account { get; }
        public Name Name { get; }
    }
}