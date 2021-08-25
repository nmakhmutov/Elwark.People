using System;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace People.Domain.Aggregates.EmailProviderAggregate
{
    public abstract class EmailProvider : Entity<EmailProvider.Type>, IAggregateRoot
    {
        public enum Type
        {
            Sendgrid = 1,
            Gmail = 2
        }
        
        protected EmailProvider(Type type, int limit, int balance)
        {
            Id = type;
            Version = int.MinValue;
            Limit = limit;
            Balance = balance;
            UpdatedAt = UpdateAt = DateTime.UtcNow;
            IsEnabled = true;
        }

        public int Version { get; set; }

        public int Limit { get; protected set; }

        public int Balance { get; protected set; }
        
        public DateTime UpdateAt { get; protected set; }
        
        public DateTime UpdatedAt { get; protected set; }

        public bool IsEnabled { get; protected set; }

        public abstract void UpdateBalance();

        public void DecreaseBalance()
        {
            if (Balance <= 0)
                throw new ElwarkException("Notification", $"'{Id}' balance is empty");

            Balance--;
            UpdateAt = DateTime.UtcNow;
        }
    }
}
