using System;
using MongoDB.Bson.Serialization;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Serializers
{
    internal sealed class LanguageSerializer : IBsonSerializer<Language>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Language value) =>
            context.Writer.WriteString(value.ToString());

        public Language Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            new(context.Reader.ReadString());

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is Language language)
                Serialize(context, args, language);
            else
                throw new NotSupportedException($"Value {value} is not correct for type '{nameof(Language)}'");
        }

        public Type ValueType { get; } = typeof(Language);
    }
}