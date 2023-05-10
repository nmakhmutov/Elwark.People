using Confluent.Kafka;

namespace People.Kafka.Converters;

internal sealed class KafkaKeyConverter :
    ISerializer<Guid>,
    IDeserializer<Guid>
{
    public static KafkaKeyConverter Instance { get; } = new();

    public Guid Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        isNull ? Guid.Empty : new Guid(data);

    public byte[] Serialize(Guid data, SerializationContext context) =>
        data.ToByteArray();
}
