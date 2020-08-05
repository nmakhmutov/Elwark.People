using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class AccountRegisteredIntegrationEvent : IntegrationEvent
    {
        public AccountRegisteredIntegrationEvent(AccountId accountId, string ipAddress, string userAgent,
            string language)
        {
            AccountId = accountId;
            IpAddress = ipAddress;
            Language = language;
            UserAgent = userAgent;
        }

        public AccountId AccountId { get; }

        public string IpAddress { get; }

        public string UserAgent { get; }

        public string Language { get; }
    }
}