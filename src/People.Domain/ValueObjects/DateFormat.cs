namespace People.Domain.ValueObjects;

public readonly struct DateFormat : IEquatable<DateFormat>
{
    public static readonly DateFormat Default = new("yyyy-MM-dd");

    private static readonly IReadOnlyCollection<string> List =
    [
        "MM.dd.yyyy",
        "dd.MM.yyyy",
        "dd.MM.yy",
        "d.M.yyyy",
        "d.M.yy",

        "MM-dd-yyyy",
        "dd-MM-yyyy",
        "dd-MM-yy",
        "d-M-yyyy",
        "d-M-yy",

        "MM/dd/yyyy",
        "dd/MM/yyyy",
        "dd/MM/yy",
        "d/M/yyyy",
        "d/M/yy",

        "yyyy-MM-dd"
    ];

    private readonly string _value;

    private DateFormat(string value) =>
        _value = value;

    public static DateFormat Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentNullException(nameof(value), "Date format cannot be null or empty.");

        if (!List.Contains(value))
            throw new ArgumentException("Date format have incorrect format.", nameof(value));

        return new DateFormat(value);
    }

    public static bool TryParse(string? value, out DateFormat format)
    {
        format = Default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!List.Contains(value))
            return false;

        format = new DateFormat(value);
        return true;
    }

    public override string ToString() =>
        _value;

    public bool Equals(DateFormat other) =>
        _value == other._value;

    public override bool Equals(object? obj) =>
        obj is DateFormat other && Equals(other);

    public override int GetHashCode() =>
        _value.GetHashCode();

    public static bool operator ==(DateFormat left, DateFormat right) =>
        left.Equals(right);

    public static bool operator !=(DateFormat left, DateFormat right) =>
        !left.Equals(right);
}
