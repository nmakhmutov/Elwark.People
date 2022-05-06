using System;
using Common.Kafka;

namespace People.Worker.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    long AccountId,
    string Email,
    string Ip,
    string Language
) : IIntegrationEvent;
