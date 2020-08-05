using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class AccountCreatedIntegrationEvent : IntegrationEvent
    {
        public AccountCreatedIntegrationEvent(AccountId accountId, Notification.PrimaryEmail primaryEmail)
        {
            AccountId = accountId;
            PrimaryEmail = primaryEmail;
        }

        public AccountId AccountId { get; }
        
        public Notification.PrimaryEmail PrimaryEmail { get; }
    }
}