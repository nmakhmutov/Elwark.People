using System;
using MongoDB.Bson;
using People.Domain.AggregateModels.Account;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Infrastructure.Confirmations
{
    public sealed class Confirmation
    {
        public Confirmation(AccountId accountId, uint code, DateTime expireAt)
        {
            Id = ObjectId.Empty;
            AccountId = accountId;
            Code = code;
            CreatedAt = DateTime.UtcNow;
            ExpireAt = expireAt;
        }

        public ObjectId Id { get; private set; }

        public AccountId AccountId { get; private set; }

        public uint Code { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime ExpireAt { get; private set; }
    }
}