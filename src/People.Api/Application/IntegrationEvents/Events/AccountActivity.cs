using System.Text.Json.Serialization;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type"),
 JsonDerivedType(typeof(InspectedIntegrationEvent), "inspected"),
 JsonDerivedType(typeof(LoggedInIntegrationEvent), "logged-in")]
public abstract record AccountActivity(Guid MessageId, DateTime CreatedAt, long AccountId)
    : IIntegrationEvent
{
    public sealed record InspectedIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) :
        AccountActivity(MessageId, CreatedAt, AccountId);

    public sealed record LoggedInIntegrationEvent(Guid MessageId, DateTime CreatedAt, long AccountId) :
        AccountActivity(MessageId, CreatedAt, AccountId);
}
