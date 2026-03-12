using System.Text;
using Confluent.Kafka;

namespace People.Kafka;

internal static class KafkaExtensions
{
    internal static string GetClientId(this Headers headers) =>
        headers.TryGetLastBytes("ClientId", out var value)
            ? Encoding.UTF8.GetString(value)
            : "Unspecified-client-id";
}
