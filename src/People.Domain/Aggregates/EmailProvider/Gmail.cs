using System;

namespace People.Domain.Aggregates.EmailProvider
{
    public sealed class Gmail : EmailProvider
    {
        public Gmail(int limit, int balance) 
            : base(EmailProviderType.Gmail, limit, balance)
        {
            ShouldUpdateAt = DateTime.Today.AddDays(1).ToUniversalTime();
        }

        public override void UpdateBalance()
        {
            if(DateTime.UtcNow.Date == UpdatedAt.Date)
                return;
            
            Balance = Limit;
            ShouldUpdateAt = ShouldUpdateAt.AddDays(1);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}