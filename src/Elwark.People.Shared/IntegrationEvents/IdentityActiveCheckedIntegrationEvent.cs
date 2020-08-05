using System;
using Elwark.EventBus;
using Elwark.People.Abstractions;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class IdentityActiveCheckedIntegrationEvent : IntegrationEvent
    {
        public IdentityActiveCheckedIntegrationEvent(IdentityId identityId, string ipAddress, DateTimeOffset checkedAt)
        {
            IdentityId = identityId;
            IpAddress = ipAddress;
            CheckedAt = checkedAt;
        }

        public IdentityId IdentityId { get; }
        
        public string IpAddress { get; }
        
        public DateTimeOffset CheckedAt { get; }
    }
}