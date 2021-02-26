using System;

namespace People.Domain.AggregateModels.EmailProvider
{
    public sealed class Sendgrid : EmailProvider
    {
        public Sendgrid(int limit, int balance)
            : base(EmailProviderType.Sendgrid, limit, balance)
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