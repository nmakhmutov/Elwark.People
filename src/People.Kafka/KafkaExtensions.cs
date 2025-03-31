using System.Diagnostics;
using System.Text;
using Confluent.Kafka;

namespace People.Kafka;

internal static class KafkaExtensions
{
    internal static ActivityTraceId GetTraceId(this Headers headers) =>
        headers.TryGetLastBytes(nameof(Activity.TraceId), out var value)
            ? ActivityTraceId.CreateFromBytes(value)
            : ActivityTraceId.CreateRandom();

    internal static ActivitySpanId GetSpanId(this Headers headers) =>
        headers.TryGetLastBytes(nameof(Activity.SpanId), out var value)
            ? ActivitySpanId.CreateFromBytes(value)
            : ActivitySpanId.CreateRandom();

    internal static string GetClientId(this Headers headers) =>
        headers.TryGetLastBytes("ClientId", out var value)
            ? Encoding.UTF8.GetString(value)
            : "Unspecified-client-id";
}
