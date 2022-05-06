using System;
using Common.Kafka;

namespace People.Api.Application.IntegrationEvents.Events;

public sealed record AccountDeletedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long Id)
    : IIntegrationEvent;