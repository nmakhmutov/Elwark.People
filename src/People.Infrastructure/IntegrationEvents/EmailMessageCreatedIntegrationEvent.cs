using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record EmailMessageCreatedIntegrationEvent(string Email, string Subject, string Body) : IKafkaMessage;
}