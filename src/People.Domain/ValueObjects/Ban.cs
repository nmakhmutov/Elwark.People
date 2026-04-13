using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.ValueObjects;

public sealed class Ban : ValueObject
{
    public string Reason { get; private set; }

    public DateTime ExpiredAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Ban(string reason, DateTime expiredAt, DateTime createdAt)
    {
        Reason = reason;
        ExpiredAt = expiredAt;
        CreatedAt = createdAt;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Reason;
        yield return CreatedAt;
        yield return ExpiredAt;
    }
}
