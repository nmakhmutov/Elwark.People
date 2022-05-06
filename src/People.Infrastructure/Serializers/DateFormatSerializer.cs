using System;
using MongoDB.Bson.Serialization;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Serializers;

internal sealed class DateFormatSerializer : IBsonSerializer<DateFormat>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateFormat value) =>
        context.Writer.WriteString(value.ToString());

    public DateFormat Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        DateFormat.Parse(context.Reader.ReadString());

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is DateFormat dateFormat)
            Serialize(context, args, dateFormat);
        else
            throw new NotSupportedException($"Value {value} is not correct for type '{nameof(DateFormat)}'");
    }

    public Type ValueType { get; } = typeof(DateFormat);
}
