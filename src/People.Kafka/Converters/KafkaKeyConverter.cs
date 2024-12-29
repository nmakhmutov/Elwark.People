using Confluent.Kafka;

namespace People.Kafka.Converters;

internal sealed class KafkaKeyConverter :
    ISerializer<Guid>,
    IDeserializer<Guid>
{
    public static readonly KafkaKeyConverter Instance = new();

    private KafkaKeyConverter()
    {
    }

    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        isNull ? Guid.Empty : new Guid(data);

    public byte[] Serialize(Guid data, SerializationContext context) =>
        data.ToByteArray();
}
