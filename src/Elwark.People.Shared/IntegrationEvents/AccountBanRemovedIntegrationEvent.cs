using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class AccountBanRemovedIntegrationEvent : IntegrationEvent
    {
        public AccountBanRemovedIntegrationEvent(AccountId accountId, Notification.PrimaryEmail email, string language)
        {
            AccountId = accountId;
            Email = email;
            Language = language;
        }

        public AccountId AccountId { get; }

        public Notification.PrimaryEmail Email { get; }

        public string Language { get; }
    }
}