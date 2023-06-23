using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.ValueObjects;

public sealed class Ban : ValueObject
{
    public Ban(string reason, DateTime expiredAt, DateTime createdAt)
    {
        Reason = reason;
        ExpiredAt = expiredAt;
        CreatedAt = createdAt;
    }

    public string Reason { get; private set; }

    public DateTime ExpiredAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Reason;
        yield return CreatedAt;
        yield return ExpiredAt;
    }
}
