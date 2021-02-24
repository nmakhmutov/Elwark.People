using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.AggregateModels.Account.Identities
{
    public abstract class IdentityModel
    {
        protected IdentityModel(Identity identity, DateTime? confirmedAt)
        {
            Type = identity.Type;
            Value = identity.Value;
            ConfirmedAt = confirmedAt;
            CreatedAt = DateTime.UtcNow;
        }

        public IdentityType Type { get; private set; }

        public string Value { get; private set; }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public abstract Identity GetIdentity();

        public bool IsConfirmed() => 
            ConfirmedAt.HasValue;
        
        public void SetAsConfirmed(DateTime confirmedAt)
        {
            ConfirmedAt = confirmedAt;
        }
    }
}