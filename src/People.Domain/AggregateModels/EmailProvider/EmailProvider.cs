using System;
using People.Domain.Exceptions;
using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace People.Domain.AggregateModels.EmailProvider
{
    public abstract class EmailProvider : Entity<EmailProviderType>, IAggregateRoot
    {
        protected EmailProvider(EmailProviderType type, int limit, int balance)
        {
            Id = type;
            Version = int.MinValue;
            Limit = limit;
            Balance = balance;
            UpdatedAt = ShouldUpdateAt = UsedAt = DateTime.UtcNow;
            IsEnabled = true;
        }

        public int Version { get; set; }

        public int Limit { get; protected set; }

        public int Balance { get; protected set; }

        public DateTime UpdatedAt { get; protected set; }

        public DateTime ShouldUpdateAt { get; protected set; }

        public DateTime UsedAt { get; protected set; }

        public bool IsEnabled { get; protected set; }

        public abstract void UpdateBalance();

        public void DecreaseBalance()
        {
            if (Balance <= 0)
                throw new ElwarkException("Notification", $"'{Id}' balance is empty");

            Balance--;
            UsedAt = DateTime.UtcNow;
        }
    }
}