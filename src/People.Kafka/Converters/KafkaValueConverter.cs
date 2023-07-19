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
    private static readonly Lazy<KafkaValueConverter<T>> Lazy = new(() => new KafkaValueConverter<T>());

    public static readonly KafkaValueConverter<T> Instance =
        Lazy.Value;

    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IgnoreReadOnlyProperties = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    private KafkaValueConverter()
    {
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, _options)!;

    public byte[] Serialize(T data, SerializationContext context)
    {
        var type = data.GetType();
        return JsonSerializer.SerializeToUtf8Bytes(data, type.BaseType ?? typeof(object), _options);
    }
}
