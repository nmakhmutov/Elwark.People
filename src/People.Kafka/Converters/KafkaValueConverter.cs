using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using People.Kafka.Integration;

namespace People.Kafka.Converters;

internal abstract class KafkaValueConverter
{
    protected static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        IgnoreReadOnlyProperties = false,
        PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };
}

internal sealed class KafkaValueConverter<T> :
    KafkaValueConverter,
    ISerializer<T>,
    IDeserializer<T>
    where T : IIntegrationEvent
{
    private static readonly Lazy<KafkaValueConverter<T>> Lazy = new(() => new KafkaValueConverter<T>());

    public static readonly KafkaValueConverter<T> Instance =
        Lazy.Value;

    private KafkaValueConverter()
    {
    }

    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        JsonSerializer.Deserialize<T>(data, Options)!;

    public byte[] Serialize(T data, SerializationContext context) =>
        JsonSerializer.SerializeToUtf8Bytes(data, data.GetType().BaseType ?? typeof(object), Options);
}
