using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using People.Kafka.Integration;

namespace People.Kafka.Converters;

internal sealed class KafkaValueConverter<T> :
    ISerializer<T>,
    IDeserializer<T>
    where T : IIntegrationEvent
{
    private readonly JsonSerializerOptions _options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false
    };

    private KafkaValueConverter()
    {
    }

    public static KafkaValueConverter<T> Instance { get; } = new();

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, _options)!;

    public byte[] Serialize(T data, SerializationContext context) =>
        JsonSerializer.SerializeToUtf8Bytes(data, typeof(T), _options);
}
