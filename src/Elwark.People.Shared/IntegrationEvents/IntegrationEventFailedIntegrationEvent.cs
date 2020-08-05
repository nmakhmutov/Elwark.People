using System;
using Elwark.EventBus;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class IntegrationEventFailedIntegrationEvent : IntegrationEvent
    {
        public IntegrationEventFailedIntegrationEvent(Guid eventLogId) =>
            EventLogId = eventLogId;

        public Guid EventLogId { get; }
    }
}