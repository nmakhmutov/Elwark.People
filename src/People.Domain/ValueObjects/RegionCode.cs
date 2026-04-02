using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct RegionCode
{
    public const int MaxLength = 2;

    private static readonly FrozenDictionary<string, string> Regions =
        new Dictionary<string, string>
            {
                ["AF"] = "Africa",
                ["AN"] = "Antarctic",
                ["AS"] = "Asia",
                ["EU"] = "Europe",
                ["NA"] = "North America",
                ["OC"] = "Oceania",
                ["SA"] = "South America"
            }
            .ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static readonly RegionCode Empty = new("--");

    private RegionCode(string? value)
    {
        if (!IsValid(value))
            throw new FormatException($"Invalid RegionCode: {value}");

        _value = value;
    }

    public bool IsEmpty() =>
        this == Empty;

    public static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length == MaxLength
        && (value == "--" || Regions.ContainsKey(value));

    public override string ToString() =>
        ValueAsString;

    public static RegionCode ParseOrDefault(string? value) =>
        TryParse(value, out var result) ? result : Empty;
}
