using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Account.Domain.Aggregates.AccountAggregate.Identities
{
    public sealed record EmailConnection : Connection
    {
        public EmailConnection(string value, DateTime createdAt, bool isPrimary, DateTime? confirmedAt = null) 
            : base(IdentityType.Email, value, createdAt, confirmedAt)
        {
            IsPrimary = isPrimary;
        }

        public bool IsPrimary { get; private set; }

        public override Email Identity => new(Value);

        internal bool SetPrimary() => IsPrimary = true;

        internal bool RemovePrimary() => IsPrimary = false;
    }
}
