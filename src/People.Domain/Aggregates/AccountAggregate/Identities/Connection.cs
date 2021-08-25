using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Identities
{
    public abstract class Connection
    {
        public enum Type
        {
            Email = 1,
            Google = 2,
            Microsoft = 3
        }
        
        protected Connection(Type connectionType, string value, DateTime? confirmedAt)
        {
            ConnectionType = connectionType;
            Value = value;
            ConfirmedAt = confirmedAt;
            CreatedAt = DateTime.UtcNow;
        }

        public Type ConnectionType { get; private set; }

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
