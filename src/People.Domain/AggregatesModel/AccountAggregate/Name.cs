using People.Domain.SeedWork;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregatesModel.AccountAggregate;

public sealed class Name : ValueObject
{
    public const int NicknameLength = 64;
    public const int FirstNameLength = 128;
    public const int LastNameLength = 128;

    public Name(string nickname, string? firstName = null, string? lastName = null, bool preferNickname = false)
    {
        if (nickname.Length > NicknameLength)
            throw new ArgumentOutOfRangeException(nameof(nickname), nickname, $"Nickname cannot be more then {NicknameLength}");

        if (firstName?.Length > FirstNameLength)
            throw new ArgumentOutOfRangeException(nameof(firstName), firstName, $"Nickname cannot be more then {FirstName}");

        if (lastName?.Length > NicknameLength)
            throw new ArgumentOutOfRangeException(nameof(lastName), lastName, $"Nickname cannot be more then {LastNameLength}");

        Nickname = nickname.Trim();
        FirstName = Normalize(firstName);
        LastName = Normalize(lastName);
        PreferNickname = preferNickname;
    }

    public string Nickname { get; private set; }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool PreferNickname { get; private set; }

    public string FullName()
    {
        if (PreferNickname || (FirstName is null && LastName is null))
            return Nickname;
        
        return $"{FirstName} {LastName}".Trim();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Nickname;
        yield return FirstName;
        yield return LastName;
        yield return PreferNickname;
    }
}
