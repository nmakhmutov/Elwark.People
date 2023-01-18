using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using People.Infrastructure.Integration;

namespace People.Infrastructure.Kafka.Converters;

internal sealed class KafkaDataConverter<T> : ISerializer<T>, IDeserializer<T>
    where T : IIntegrationEvent
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
    };

    private KafkaDataConverter()
    {
    }

    public static KafkaDataConverter<T> Instance { get; } = new();

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, _options)!;

    public byte[] Serialize(T data, SerializationContext context) =>
        JsonSerializer.SerializeToUtf8Bytes(data, typeof(T), _options);
}
