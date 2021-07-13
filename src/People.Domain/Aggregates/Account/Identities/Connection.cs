using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.Account.Identities
{
    public abstract class Connection
    {
        protected Connection(IdentityType identityType, string value, DateTime? confirmedAt)
        {
            IdentityType = identityType;
            Value = value;
            ConfirmedAt = confirmedAt;
            CreatedAt = DateTime.UtcNow;
        }

        public IdentityType IdentityType { get; private set; }

        public string Value { get; private set; }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public abstract Identity Identity { get; }

        public bool IsConfirmed => 
            ConfirmedAt.HasValue;
        
        public void SetAsConfirmed(DateTime confirmedAt)
        {
            ConfirmedAt = confirmedAt;
        }
    }
}
