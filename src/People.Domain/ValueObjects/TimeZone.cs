namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct TimeZone
{
    public const int MaxLength = 128;

    public static readonly TimeZone Utc =
        new(TimeZoneInfo.Utc.Id);

    private TimeZone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException($"Invalid TimeZone: {value}");

        _value = TimeZoneInfo.FindSystemTimeZoneById(value).Id;
    }

    public override string ToString() =>
        ValueAsString;

    public static TimeZone ParseOrDefault(string? value) =>
        TryParse(value, out var result) ? result : Utc;
}
