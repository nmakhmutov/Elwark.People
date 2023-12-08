using System.Collections.Frozen;

namespace People.Domain.ValueObjects;

public readonly struct RegionCode :
    IComparable,
    IComparable<RegionCode>,
    IEquatable<RegionCode>
{
    private static readonly FrozenDictionary<string, string> Regions =
        new Dictionary<string, string>
            {
                ["AF"] = "Africa",
                ["AN"] = "Antarctica",
                ["AS"] = "Asia",
                ["EU"] = "Europe",
                ["NA"] = "North America",
                ["OC"] = "Oceania",
                ["SA"] = "South America"
            }
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static RegionCode Empty =>
        new("--");

    private readonly string _value;

    private RegionCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Region code cannot be null or whitespace.", nameof(value));

        if (value.Length != 2)
            throw new ArgumentException($"Region code has wrong format '{value}'", nameof(value));

        _value = value.ToUpperInvariant();
    }

    public static RegionCode Parse(string? value)
    {
        if (TryParse(value, out var code))
            return code;

        throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(RegionCode)} value must be two chars");
    }

    public static bool TryParse(string? value, out RegionCode regionCode)
    {
        regionCode = Empty;

        if (string.IsNullOrWhiteSpace(value) || value.Length != 2)
            return false;

        regionCode = GetCodeOrDefault(value);

        return true;
    }

    private static RegionCode GetCodeOrDefault(string value)
    {
        if (value == "--")
            return Empty;

        if (value.Length == 2)
            return Regions.ContainsKey(value) ? new RegionCode(value) : Empty;

        foreach (var (code, name) in Regions)
            if (name.Equals(value, StringComparison.OrdinalIgnoreCase))
                return new RegionCode(code);

        return Empty;
    }

    public bool IsEmpty() =>
        this == Empty;

    public bool Equals(RegionCode other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is RegionCode other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public override string ToString() =>
        _value;

    public int CompareTo(RegionCode other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return 1;

        return obj is RegionCode other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(RegionCode)}");
    }

    public static bool operator <(RegionCode left, RegionCode right) =>
        left.CompareTo(right) < 0;

    public static bool operator >(RegionCode left, RegionCode right) =>
        left.CompareTo(right) > 0;

    public static bool operator <=(RegionCode left, RegionCode right) =>
        left.CompareTo(right) <= 0;

    public static bool operator >=(RegionCode left, RegionCode right) =>
        left.CompareTo(right) >= 0;

    public static bool operator ==(RegionCode left, RegionCode right) =>
        left.Equals(right);

    public static bool operator !=(RegionCode left, RegionCode right) =>
        !left.Equals(right);
}
