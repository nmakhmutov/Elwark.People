using System.Diagnostics.CodeAnalysis;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct Language
{
    public const int MaxLength = 2;

    public static Language Default =>
        new("en");

    private Language(string value)
    {
        if (!IsValid(value))
            throw new FormatException($"Invalid Language: {value}");

        _value = value;
    }

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length == MaxLength
        && !value.Equals("iv", StringComparison.CurrentCultureIgnoreCase);

    public override string ToString() =>
        ValueAsString;
}
