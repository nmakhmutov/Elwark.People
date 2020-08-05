using System;
using Elwark.EventBus;
using Elwark.People.Abstractions;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class ConfirmationByUrlCreatedIntegrationEvent : IntegrationEvent
    {
        public ConfirmationByUrlCreatedIntegrationEvent(Notification notification, Uri url, string language,
            ConfirmationType confirmationType)
        {
            Notification = notification;
            Url = url;
            Language = language;
            ConfirmationType = confirmationType;
        }

        public Notification Notification { get; }

        public Uri Url { get; }

        public string Language { get; }

        public ConfirmationType ConfirmationType { get; }
    }
}