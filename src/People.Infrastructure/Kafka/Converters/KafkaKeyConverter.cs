using System.Text;
using Confluent.Kafka;

namespace People.Infrastructure.Kafka.Converters;

internal sealed class KafkaKeyConverter : ISerializer<string>, IDeserializer<string>
{
    public static KafkaKeyConverter Instance { get; } = new();

    public string Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context) =>
        Encoding.UTF8.GetString(data);

    public byte[] Serialize(string data, SerializationContext context) =>
        Encoding.UTF8.GetBytes(data);
}
