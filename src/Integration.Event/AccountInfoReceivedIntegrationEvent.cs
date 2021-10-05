using Common.Kafka;

namespace Integration.Event;

public sealed record AccountInfoReceivedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    long AccountId,
    string Ip,
    string? CountryCode,
    string? City,
    string? TimeZone,
    string? FirstName,
    string? LastName,
    string? AboutMe,
    Uri? Image
) : IKafkaMessage;
