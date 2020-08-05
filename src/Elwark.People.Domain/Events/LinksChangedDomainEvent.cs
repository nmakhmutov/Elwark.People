using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class LinksChangedDomainEvent : INotification
    {
        public LinksChangedDomainEvent(Account account, Links links)
        {
            Account = account;
            Links = links;
        }

        public Account Account { get; }
        public Links Links { get; }
    }
}