using Common.Kafka;

namespace Integration.Event;

public sealed record AccountCreatedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    long AccountId,
    string Email,
    string Ip,
    string Language
) : IKafkaMessage;
