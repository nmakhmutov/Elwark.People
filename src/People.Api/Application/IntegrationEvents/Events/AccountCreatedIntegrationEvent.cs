using System;
using Common.Kafka;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountCreatedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    long AccountId,
    string Email,
    string Ip,
    string Language
) : IIntegrationEvent;
