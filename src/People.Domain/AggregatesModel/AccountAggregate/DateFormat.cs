namespace People.Domain.AggregatesModel.AccountAggregate;

public readonly struct DateFormat : IEquatable<DateFormat>
{
    public static readonly DateFormat Default = new("yyyy-MM-dd");

    public static readonly IReadOnlyCollection<string> List = new[]
    {
        "MM/dd/yyyy",
        "MM.dd.yyyy",
        "MM-dd-yyyy",
        "dd/MM/yyyy",
        "dd.MM.yyyy",
        "dd-MM-yyyy",
        "yyyy-MM-dd"
    };

    private readonly string _value;

    private DateFormat(string value) =>
        _value = value;

    public static DateFormat Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentNullException(nameof(value), "Date format cannot be null or empty.");

        if (!List.Contains(value))
            throw new ArgumentException("Date format have incorrect format.", nameof(value));

        return new DateFormat(value);
    }

    public static bool TryParse(string? value, out DateFormat dateFormat)
    {
        dateFormat = Default;

        if (string.IsNullOrEmpty(value))
            return false;

        if (!List.Contains(value))
            return false;

        dateFormat = new DateFormat(value);
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
