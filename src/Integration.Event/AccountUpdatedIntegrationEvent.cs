using Common.Kafka;

namespace Integration.Event;

public sealed record AccountUpdatedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id)
    : IKafkaMessage;
