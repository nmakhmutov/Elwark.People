using System;
using MongoDB.Bson.Serialization;
using TimeZone = People.Domain.Aggregates.AccountAggregate.TimeZone;

namespace People.Infrastructure.Serializers;

internal sealed class TimeZoneSerializer : IBsonSerializer<TimeZone>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeZone value) =>
        context.Writer.WriteString(value.ToString());

    public TimeZone Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        TimeZone.Parse(context.Reader.ReadString());

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is TimeZone timeZone)
            Serialize(context, args, timeZone);
        else
            throw new NotSupportedException($"Value {value} is not correct for type '{nameof(TimeZone)}'");
    }

    public Type ValueType { get; } = typeof(TimeZone);
}
