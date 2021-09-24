using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Account.Domain.Aggregates.AccountAggregate.Identities
{
    public abstract record Connection : Identity
    {
        protected Connection(IdentityType type, string value, DateTime createdAt, DateTime? confirmedAt)
            : base(type, value)
        {
            Value = value;
            ConfirmedAt = confirmedAt;
            CreatedAt = createdAt;
        }

        public DateTime? ConfirmedAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public bool IsConfirmed =>
            ConfirmedAt.HasValue;

        public void SetAsConfirmed(DateTime confirmedAt) =>
            ConfirmedAt = confirmedAt;

        public abstract Identity Identity { get; }
    }
}
