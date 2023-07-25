namespace People.Domain.Entities;

public readonly struct AccountId :
    IEquatable<AccountId>,
    IComparable<AccountId>
{
    private readonly long _value;

    public AccountId(long userId)
    {
        if (userId < 0)
            throw new ArgumentOutOfRangeException(nameof(userId), userId, null);

        _value = userId;
    }

    public int CompareTo(AccountId other) =>
        _value.CompareTo(other._value);

    public int CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            AccountId id => CompareTo(id),
            _ => throw new ArgumentException($"Object is not {nameof(AccountId)}")
        };

    public bool Equals(AccountId other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is AccountId other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public override string ToString() =>
        _value.ToString();

    public static bool operator ==(AccountId a, AccountId b) =>
        a.CompareTo(b) == 0;

    public static bool operator !=(AccountId a, AccountId b) =>
        !(a == b);

    public static bool operator >(AccountId a, AccountId b) =>
        a._value > b._value;

    public static bool operator <(AccountId a, AccountId b) =>
        a._value < b._value;

    public static bool operator <=(AccountId a, AccountId b) =>
        a._value <= b._value;

    public static bool operator >=(AccountId a, AccountId b) =>
        a._value >= b._value;

    public static implicit operator long(AccountId id) =>
        id._value;

    public static implicit operator AccountId(long id) =>
        new(id);
}
