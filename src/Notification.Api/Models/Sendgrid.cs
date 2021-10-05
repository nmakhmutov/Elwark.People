namespace Notification.Api.Models;

public sealed class Sendgrid : EmailProvider
{
    public Sendgrid(int limit, int balance)
        : base(Type.Sendgrid, limit, balance) =>
        UpdateAt = DateTime.Today.AddDays(1).ToUniversalTime();

    public override void UpdateBalance()
    {
        if (DateTime.UtcNow.Date == UpdatedAt.Date)
            return;

        Balance = Limit;
        UpdateAt = UpdateAt.AddDays(1);
        UpdatedAt = DateTime.UtcNow;
    }
}
