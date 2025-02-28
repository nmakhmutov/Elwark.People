using System.Text.Json.Serialization;
using People.Kafka;
using People.Kafka.Integration;

namespace People.Api.Application.IntegrationEvents.Events;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type"),
 JsonDerivedType(typeof(InspectedIntegrationEvent), "inspected"),
 JsonDerivedType(typeof(LoggedInIntegrationEvent), "logged-in")]
public abstract record AccountActivity(long AccountId) : IntegrationEvent, IKafkaMessage
{
    public string GetTopicKey() =>
        AccountId.ToString();

    public sealed record InspectedIntegrationEvent(long AccountId) : AccountActivity(AccountId);

    public sealed record LoggedInIntegrationEvent(long AccountId) : AccountActivity(AccountId);
}
