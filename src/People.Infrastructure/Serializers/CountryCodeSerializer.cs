using System;
using MongoDB.Bson.Serialization;
using People.Domain.Aggregates.AccountAggregate;

namespace People.Infrastructure.Serializers;

internal sealed class CountryCodeSerializer : IBsonSerializer<CountryCode>
{
    object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        Deserialize(context, args);

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CountryCode value) =>
        context.Writer.WriteString(value.ToString());

    public CountryCode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        new(context.Reader.ReadString());

    public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
    {
        if (value is CountryCode code)
            Serialize(context, args, code);
        else
            throw new NotSupportedException($"Value {value} is not correct for type '{nameof(CountryCode)}'");
    }

    public Type ValueType { get; } = typeof(CountryCode);
}
