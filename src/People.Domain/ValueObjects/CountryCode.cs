using System.Diagnostics.CodeAnalysis;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct CountryCode
{
    public const int MaxLength = 2;

    public static readonly CountryCode Empty = new("--");

    private CountryCode(string value)
    {
        if (!IsValid(value))
            throw new FormatException($"Invalid CountryCode: {value}");

        _value = value.ToUpper();
    }

    public bool IsEmpty() =>
        this == Empty;

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length == MaxLength;

    public override string ToString() =>
        ValueAsString;

    public static CountryCode ParseOrDefault(string? value) =>
        TryParse(value, out var result) ? result : Empty;
}
