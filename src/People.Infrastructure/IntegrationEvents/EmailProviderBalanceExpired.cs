using People.Domain.AggregateModels.EmailProvider;
using People.Infrastructure.Kafka;

namespace People.Infrastructure.IntegrationEvents
{
    public sealed record ProviderExpiredIntegrationEvent(EmailProviderType Type) : IKafkaMessage;
}