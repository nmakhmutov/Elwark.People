using System;
using MongoDB.Bson;
using People.Domain.AggregateModels.Account;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Infrastructure.Confirmations
{
    public sealed class Confirmation
    {
        public Confirmation(AccountId accountId, ConfirmationType type, int code, TimeSpan lifetime)
        {
            Id = ObjectId.Empty;
            AccountId = accountId;
            Code = code;
            Type = type;
            CreatedAt = DateTime.UtcNow;
            ExpireAt = CreatedAt.Add(lifetime);
        }

        public ObjectId Id { get; private set; }

        public AccountId AccountId { get; private set; }

        public ConfirmationType Type { get; private set; }

        public int Code { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime ExpireAt { get; private set; }
    }

    public enum ConfirmationType
    {
        SignUp,
        ResetPassword
    }
}