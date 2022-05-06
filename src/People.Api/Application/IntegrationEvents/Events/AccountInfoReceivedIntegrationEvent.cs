using System;
using Common.Kafka;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountInfoReceivedIntegrationEvent(
    Guid MessageId,
    DateTime CreatedAt,
    long AccountId,
    string Ip,
    string? CountryCode,
    string? TimeZone,
    string? FirstName,
    string? LastName,
    string? Image
) : IIntegrationEvent;
