using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record AccountCreatedIntegrationEvent(long Id, string Email, string Ip, string Language) 
        : IKafkaMessage;
}