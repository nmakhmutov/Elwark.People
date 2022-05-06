using Common.Kafka;

namespace Notification.Api.IntegrationEvents.Events;

public sealed record EmailMessageCreatedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    string Email,
    string Subject,
    string Body,
    bool IsDurable
) : IIntegrationEvent
{
    public static EmailMessageCreatedIntegrationEvent CreateDurable(string email, string subject, string body) =>
        new(Guid.NewGuid(), DateTime.UtcNow, email, subject, body, true);

    public static EmailMessageCreatedIntegrationEvent CreateNotDurable(string email, string subject, string body) =>
        new(Guid.NewGuid(), DateTime.UtcNow, email, subject, body, false);
}
