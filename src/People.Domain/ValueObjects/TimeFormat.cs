using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct TimeFormat
{
    public const int MaxLength = 32;

    private static readonly FrozenSet<string> List =
    [
        "H:mm",
        "HH:mm",
        "HH:mm:ss",
        "h:mm tt",
        "hh:mm tt"
    ];

    public static readonly TimeFormat Default = new("HH:mm");

    public static TimeFormat Convert(CultureInfo culture)
    {
        var format = culture.DateTimeFormat.ShortTimePattern;
        return IsValid(format) ? new TimeFormat(format) : Default;
    }

    private TimeFormat(string? value)
    {
        if (!IsValid(value))
            throw new FormatException($"Invalid TimeFormat: {value}");

        _value = value;
    }

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && List.Contains(value);

    public override string ToString() =>
        ValueAsString;
}
