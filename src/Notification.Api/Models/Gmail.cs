namespace Notification.Api.Models;

public sealed class Gmail : EmailProvider
{
    public Gmail(int limit, int balance)
        : base(Type.Gmail, limit, balance) =>
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
