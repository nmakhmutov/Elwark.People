using People.Kafka;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountDeletedIntegrationEvent(long AccountId) : IntegrationEvent, IKafkaMessage
{
    public string GetTopicKey() =>
        AccountId.ToString();
}
