using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct DateFormat
{
    public const int MaxLength = 32;

    private static readonly FrozenSet<string> List =
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

    public static readonly DateFormat Default = new("yyyy-MM-dd");

    public static DateFormat Convert(CultureInfo culture)
    {
        var format = culture.DateTimeFormat.ShortDatePattern;
        return IsValid(format) ? new DateFormat(format) : Default;
    }

    private DateFormat(string value)
    {
        if (!IsValid(value))
            throw new FormatException($"Invalid DateFormat: {value}");

        _value = value;
    }

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && List.Contains(value);

    public override string ToString() =>
        ValueAsString;
}
