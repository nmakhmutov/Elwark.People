using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class AddressChangedDomainEvent : INotification
    {
        public AddressChangedDomainEvent(Account account, Address address)
        {
            Account = account;
            Address = address;
        }

        public Account Account { get; }
        public Address Address { get; }
    }
}