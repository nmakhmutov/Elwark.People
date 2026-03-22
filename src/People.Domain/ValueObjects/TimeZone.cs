namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct TimeZone
{
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
}
