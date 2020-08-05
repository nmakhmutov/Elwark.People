using System;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using MediatR;

namespace Elwark.People.Domain.Events
{
    public class RoleAddedDomainEvent : INotification
    {
        public RoleAddedDomainEvent(Account account, string roleName)
        {
            Account = account ?? throw new ArgumentNullException(nameof(account));
            RoleName = roleName ?? throw new ArgumentNullException(nameof(roleName));
        }

        public Account Account { get; }
        public string RoleName { get; }
    }
}