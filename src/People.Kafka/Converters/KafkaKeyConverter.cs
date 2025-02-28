using Confluent.Kafka;

namespace People.Kafka.Converters;

internal sealed class KafkaKeyConverter :
    ISerializer<string>,
    IDeserializer<string>
{
    public static readonly KafkaKeyConverter Instance = new();

    private KafkaKeyConverter()
    {
    }

    public string Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        Deserializers.Utf8.Deserialize(data, isNull, context);

    public byte[] Serialize(string data, SerializationContext context) =>
        Serializers.Utf8.Serialize(data, context);
}
