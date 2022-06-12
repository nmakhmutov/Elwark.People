namespace People.Domain.AggregatesModel.AccountAggregate;

public readonly struct Language : IComparable, IComparable<Language>, IEquatable<Language>
{
    public static Language Default =>
        new("en");

    private readonly string _value;

    private Language(string value) =>
        _value = value.ToLowerInvariant();

    public static Language Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), $"{nameof(Language)} cannot be empty");

        if (value.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(Language)} value must be two chars");

        if ("iv".Equals(value, StringComparison.InvariantCultureIgnoreCase))
            throw new ArgumentException("Language cannot be invariant", nameof(value));

        return new Language(value);
    }

    public static bool TryParse(string? value, out Language language)
    {
        language = Default;

        if (string.IsNullOrEmpty(value))
            return false;

        if (value.Length != 2)
            return false;

        if ("iv".Equals(value, StringComparison.InvariantCultureIgnoreCase))
            return false;

        language = new Language(value);
        return true;
    }

    public bool Equals(Language other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is Language other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public override string ToString() =>
        _value;

    public int CompareTo(Language other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return 1;

        return obj is Language other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(Language)}");
    }

    public static bool operator <(Language left, Language right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(Language left, Language right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(Language left, Language right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(Language left, Language right) =>
        left.CompareTo(right) >= 0;

    public static bool operator ==(Language left, Language right) =>
        left.Equals(right);

    public static bool operator !=(Language left, Language right) =>
        !left.Equals(right);
}
