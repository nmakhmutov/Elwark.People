namespace People.Domain.ValueObjects;

public readonly struct ContinentCode :
    IComparable,
    IComparable<ContinentCode>,
    IEquatable<ContinentCode>
{
    private static readonly Dictionary<string, string> Continents = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AF"] = "Africa",
        ["AN"] = "Antarctica",
        ["AS"] = "Asia",
        ["EU"] = "Europe",
        ["NA"] = "North America",
        ["OC"] = "Oceania",
        ["SA"] = "South America"
    };

    public static ContinentCode Empty =>
        new("--");

    private readonly string _value;

    private ContinentCode(string value) =>
        _value = value.ToUpperInvariant();

    public static ContinentCode Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), $"{nameof(ContinentCode)} cannot be empty");

        if (value.Length != 2)
            throw new ArgumentException($"{nameof(ContinentCode)} value must be two chars", nameof(value));

        return new ContinentCode(value);
    }

    public static bool TryParse(string? value, out ContinentCode continentCode)
    {
        continentCode = Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (value.Length != 2)
            return false;

        if (!Continents.ContainsKey(value))
            return false;

        continentCode = new ContinentCode(value);
        return true;
    }

    public bool IsEmpty() =>
        this == Empty;

    public bool Equals(ContinentCode other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is ContinentCode other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public override string ToString() =>
        _value;

    public int CompareTo(ContinentCode other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return 1;

        return obj is ContinentCode other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(ContinentCode)}");
    }

    public static bool operator <(ContinentCode left, ContinentCode right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(ContinentCode left, ContinentCode right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(ContinentCode left, ContinentCode right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(ContinentCode left, ContinentCode right) =>
        left.CompareTo(right) >= 0;

    public static bool operator ==(ContinentCode left, ContinentCode right) =>
        left.Equals(right);

    public static bool operator !=(ContinentCode left, ContinentCode right) =>
        !left.Equals(right);
}
