using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class AccountBanExpiredIntegrationEvent : IntegrationEvent
    {
        public AccountBanExpiredIntegrationEvent(AccountId accountId) =>
            AccountId = accountId;

        public AccountId AccountId { get; }
    }
}