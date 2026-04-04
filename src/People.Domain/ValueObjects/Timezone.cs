namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct Timezone
{
    public const int MaxLength = 128;

    public static readonly Timezone Utc =
        new(TimeZoneInfo.Utc.Id);

    private Timezone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new FormatException($"Invalid TimeZone: {value}");

        try
        {
            _value = TimeZoneInfo.FindSystemTimeZoneById(value).Id;
        }
        catch
        {
            _value = TimeZoneInfo.Utc.Id;
        }
    }

    public override string ToString() =>
        ValueAsString;

    public static Timezone ParseOrDefault(string? value) =>
        TryParse(value, out var result) ? result : Utc;
}
