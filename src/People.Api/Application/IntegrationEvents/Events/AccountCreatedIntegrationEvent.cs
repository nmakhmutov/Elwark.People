using People.Kafka;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(long AccountId, string Ip) : IntegrationEvent, IKafkaMessage
{
    public string GetTopicKey() =>
        AccountId.ToString();
}
