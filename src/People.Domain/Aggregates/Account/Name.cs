using System;

namespace People.Domain.Aggregates.Account
{
    public sealed record Name
    {
        public const int NicknameLength = 99;
        public const int FirstNameLength = 99;
        public const int LastNameLength = 99;
        public const int FullNameLength = FirstNameLength + LastNameLength;

        public Name(string nickname, string? firstName = null, string? lastName = null, bool preferNickname = false)
        {
            if (nickname.Length > NicknameLength)
                throw new ArgumentOutOfRangeException(nameof(nickname), nickname,
                    $"Nickname cannot be more then {NicknameLength}");

            if (firstName?.Length > FirstNameLength)
                throw new ArgumentOutOfRangeException(nameof(firstName), firstName,
                    $"Nickname cannot be more then {FirstName}");

            if (lastName?.Length > NicknameLength)
                throw new ArgumentOutOfRangeException(nameof(lastName), lastName,
                    $"Nickname cannot be more then {LastNameLength}");

            Nickname = nickname;
            FirstName = firstName;
            LastName = lastName;
            PreferNickname = preferNickname;
        }

        public string Nickname { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public bool PreferNickname { get; init; }

        public string FullName()
        {
            if (PreferNickname)
                return Nickname;

            var fullName = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrEmpty(fullName) ? Nickname : fullName;
        }
    }
}
