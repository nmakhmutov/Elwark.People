using Elwark.EventBus;

namespace Elwark.People.Shared.IntegrationEvents
{
    public class EmailCreatedIntegrationEvent : IntegrationEvent
    {
        public EmailCreatedIntegrationEvent(string email, string subject, string body)
        {
            Email = email;
            Subject = subject;
            Body = body;
        }

        public string Email { get; }

        public string Subject { get; }

        public string Body { get; }
    }
}