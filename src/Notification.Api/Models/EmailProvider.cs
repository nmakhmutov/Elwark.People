

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace Notification.Api.Models;

public abstract class EmailProvider
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

    public Type Id { get; protected set; }

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
            throw new Exception($"'{Id}' balance is empty");

        Balance--;
        UpdatedAt = DateTime.UtcNow;
    }
}
