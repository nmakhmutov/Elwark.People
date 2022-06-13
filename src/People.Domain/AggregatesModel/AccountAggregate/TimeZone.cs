namespace People.Domain.AggregatesModel.AccountAggregate;

public readonly struct TimeZone : IComparable, IComparable<TimeZone>, IEquatable<TimeZone>
{
    public static readonly TimeZone Utc =
        new(TimeZoneInfo.Utc.Id);

    private readonly string _value;

    private TimeZone(string value) =>
        _value = value;

    public static TimeZone Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or empty.", nameof(value));

        return new TimeZone(TimeZoneInfo.FindSystemTimeZoneById(value).Id);
    }

    public static bool TryParse(string? value, out TimeZone timeZone)
    {
        timeZone = Utc;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            timeZone = new TimeZone(TimeZoneInfo.FindSystemTimeZoneById(value).Id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public override string ToString() =>
        _value;

    public bool Equals(TimeZone other) =>
        _value == other._value;

    public int CompareTo(TimeZone other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return 1;

        return obj is TimeZone other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(TimeZone)}");
    }

    public static bool operator <(TimeZone left, TimeZone right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(TimeZone left, TimeZone right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(TimeZone left, TimeZone right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(TimeZone left, TimeZone right) =>
        left.CompareTo(right) >= 0;

    public override bool Equals(object? obj) =>
        obj is TimeZone other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public static bool operator ==(TimeZone left, TimeZone right) =>
        left.Equals(right);

    public static bool operator !=(TimeZone left, TimeZone right) =>
        !left.Equals(right);
}
