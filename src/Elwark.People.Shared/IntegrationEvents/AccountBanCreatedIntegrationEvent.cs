using Elwark.EventBus;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class AccountBanCreatedIntegrationEvent : IntegrationEvent
    {
        public AccountBanCreatedIntegrationEvent(AccountId accountId, Notification.PrimaryEmail email, BanType type,
            string reason, string language)
        {
            AccountId = accountId;
            Reason = reason;
            Language = language;
            Type = type;
            Email = email;
        }

        public AccountId AccountId { get; }

        public Notification.PrimaryEmail Email { get; }

        public BanType Type { get; }
        
        public string Reason { get; }

        public string Language { get; }
    }
}