using System.Text.Json.Serialization;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type"),
 JsonDerivedType(typeof(CheckedActivityIntegrationEvent), "checked"),
 JsonDerivedType(typeof(LoggedInIntegrationEvent), "login")]
public abstract record AccountEngaged(Guid MessageId, DateTime CreatedAt, long AccountId)
    : IIntegrationEvent
{
    public sealed record CheckedActivityIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) :
        AccountEngaged(MessageId, CreatedAt, AccountId);

    public sealed record LoggedInIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) :
        AccountEngaged(MessageId, CreatedAt, AccountId);
}
