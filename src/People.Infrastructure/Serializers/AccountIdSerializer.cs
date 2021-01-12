using System;
using MongoDB.Bson.Serialization;
using People.Domain.AggregateModels.Account;

namespace People.Infrastructure.Serializers
{
    internal sealed class AccountIdSerializer : IBsonSerializer<AccountId>
    {
        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            Deserialize(context, args);

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AccountId value) =>
            context.Writer.WriteInt64((long) value);

        public AccountId Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
            new(context.Reader.ReadInt64());

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            if (value is AccountId id)
                Serialize(context, args, id);
            else
                throw new NotSupportedException($"Value {value} is not correct for type '${nameof(AccountId)}'");
        }

        public Type ValueType { get; } = typeof(AccountId);
    }
}