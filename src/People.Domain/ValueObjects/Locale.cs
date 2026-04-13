using System.Globalization;

namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct Locale
{
    public const int MaxLength = 12;

    public string Language =>
        _value[..2].ToLower();

    public override string ToString() =>
        ValueAsString;

    public static Locale FromCulture(CultureInfo culture) =>
        new(culture.Name);
}
