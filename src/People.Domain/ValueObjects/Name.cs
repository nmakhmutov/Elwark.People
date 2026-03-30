using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.ValueObjects;

public sealed class Name : ValueObject
{
    public const int NicknameLength = 64;
    public const int FirstNameLength = 128;
    public const int LastNameLength = 128;

    public string Nickname { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool PreferNickname { get; private set; }

    private Name(string nickname, string? firstName, string? lastName, bool preferNickname)
    {
        Nickname = nickname;
        FirstName = firstName;
        LastName = lastName;
        PreferNickname = preferNickname;
    }

    public static Name Create(
        string nickname,
        string? firstName = null,
        string? lastName = null,
        bool preferNickname = true
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nickname);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(nickname.Length, NicknameLength, nameof(nickname));

        if (firstName is not null)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstName.Length, FirstNameLength, nameof(firstName));

        if (lastName is not null)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lastName.Length, LastNameLength, nameof(lastName));

        return new Name(nickname.Trim(), firstName?.Trim(), lastName?.Trim(), preferNickname);
    }

    public string FullName()
    {
        if (PreferNickname || (FirstName is null && LastName is null))
            return Nickname;

        return $"{FirstName} {LastName}".Trim();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nickname;
        yield return FirstName;
        yield return LastName;
        yield return PreferNickname;
    }
}
