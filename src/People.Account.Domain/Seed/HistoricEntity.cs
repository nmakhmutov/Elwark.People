using System;

namespace People.Account.Domain.Seed
{
    public abstract class HistoricEntity<T> : Entity<T>, IHasHistory where T : struct
    {
        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }

        public void SetAsCreated(DateTime date)
        {
            if (CreatedAt == DateTime.MinValue)
                CreatedAt = date;

            UpdatedAt = date;
        }

        public void SetAsUpdated(DateTime date) =>
            UpdatedAt = date;
    }
}
