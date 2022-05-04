using System;
using MongoDB.Bson.Serialization;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Serializers;

internal sealed class TimeFormatSerializer : IBsonSerializer<TimeFormat>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeFormat value) =>
        context.Writer.WriteString(value.ToString());

    public TimeFormat Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        TimeFormat.Parse(context.Reader.ReadString());

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is TimeFormat timeFormat)
            Serialize(context, args, timeFormat);
        else
            throw new NotSupportedException($"Value {value} is not correct for type '{nameof(TimeFormat)}'");
    }

    public Type ValueType { get; } = typeof(TimeFormat);
}
