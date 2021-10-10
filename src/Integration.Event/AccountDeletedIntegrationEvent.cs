using Common.Kafka;

namespace Integration.Event;

public sealed record AccountDeletedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id)
    : IKafkaMessage;
