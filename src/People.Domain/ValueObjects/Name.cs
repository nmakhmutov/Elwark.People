using People.Domain.SeedWork;

namespace People.Domain.ValueObjects;

public sealed class Name : ValueObject
{
    public const int FirstNameLength = 128;
    public const int LastNameLength = 128;

    public Nickname Nickname { get; }

    public string? FirstName { get; }

    public string? LastName { get; }

    public bool PreferNickname { get; }

    private Name(Nickname nickname, string? firstName, string? lastName, bool preferNickname)
    {
        Nickname = nickname;
        FirstName = firstName;
        LastName = lastName;
        PreferNickname = preferNickname;
    }

    public static Name Create(Nickname nickname, string? firstName = null, string? lastName = null, bool preferNickname = true)
    {
        firstName = firstName.TrimToNull();
        lastName = lastName.TrimToNull();

        if (firstName is not null)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(firstName.Length, FirstNameLength, nameof(firstName));

        if (lastName is not null)
            ArgumentOutOfRangeException.ThrowIfGreaterThan(lastName.Length, LastNameLength, nameof(lastName));

        return new Name(nickname, firstName, lastName, preferNickname);
    }

    public string FullName()
    {
        if (PreferNickname || (FirstName is null && LastName is null))
            return Nickname.ToString();

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
