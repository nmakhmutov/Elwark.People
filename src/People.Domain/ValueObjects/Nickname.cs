namespace People.Domain.ValueObjects;

[StronglyTypedId<string>(generateNewtonsoftJsonConverter: false, generateMongoDBBsonSerialization: false)]
public readonly partial struct Nickname
{
    public const int MaxLength = 64;

    private Nickname(string? nickname)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nickname.Length, MaxLength, nameof(nickname));

        _value = nickname.Trim();
    }

    public override string ToString() =>
        _value;
}
