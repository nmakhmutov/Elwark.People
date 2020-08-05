using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class BasicInfoChangedDomainEvent : INotification
    {
        public BasicInfoChangedDomainEvent(Account account, BasicInfo info)
        {
            Account = account;
            Info = info;
        }

        public Account Account { get; }
        public BasicInfo Info { get; }
    }
}