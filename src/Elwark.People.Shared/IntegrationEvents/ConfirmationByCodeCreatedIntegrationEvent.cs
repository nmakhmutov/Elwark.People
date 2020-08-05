using Elwark.EventBus;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class ConfirmationByCodeCreatedIntegrationEvent : IntegrationEvent
    {
        public ConfirmationByCodeCreatedIntegrationEvent(Notification notification, long code, string language,
            ConfirmationType confirmationType)
        {
            Notification = notification;
            Code = code;
            Language = language;
            ConfirmationType = confirmationType;
        }

        public Notification Notification { get; }

        public long Code { get; }

        public string Language { get; }

        public ConfirmationType ConfirmationType { get; }
    }
}