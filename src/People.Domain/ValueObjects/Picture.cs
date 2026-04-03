namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct Picture
{
    public const int MaxLength = 2048;
    private const string DefaultPicture = "https://res.cloudinary.com/elwark/image/upload/v1/People/default.jpg";

    public static readonly Picture Default = new(DefaultPicture);

    private Picture(string? value) =>
        _value = string.IsNullOrWhiteSpace(value) ? DefaultPicture : value;

    public static Picture Parse(Uri picture) =>
        new(picture.AbsoluteUri);

    public override string ToString() =>
        _value;
}
