using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.AggregateModels.Account.Identities
{
    public abstract class Identity
    {
        protected Identity(IdentityKey key)
        {
            Type = key.Type;
            Value = key.Value;
            ConfirmedAt = null;
            CreatedAt = DateTime.UtcNow;
        }

        public IdentityType Type { get; private set; }

        public string Value { get; private set; }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public IdentityKey GetKey() => 
            new(Type, Value);

        public bool IsConfirmed() => ConfirmedAt.HasValue;
        
        public Identity SetAsConfirmed(DateTime confirmedAt)
        {
            if (IsConfirmed())
                return this;

            ConfirmedAt = confirmedAt;

            return this;
        }
    }
}