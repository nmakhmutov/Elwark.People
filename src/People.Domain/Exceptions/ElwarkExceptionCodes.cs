namespace People.Domain.Exceptions
{
    public static class ElwarkExceptionCodes
    {
        public const string AccountAlreadyExists = nameof(AccountAlreadyExists);
        
        public const string PasswordEmpty = nameof(PasswordEmpty);
        public const string PasswordTooShort = nameof(PasswordTooShort);
        public const string PasswordRequiresNonAlphanumeric = nameof(PasswordRequiresNonAlphanumeric);
        public const string PasswordRequiresDigit = nameof(PasswordRequiresDigit);
        public const string PasswordRequiresLower = nameof(PasswordRequiresLower);
        public const string PasswordRequiresUpper = nameof(PasswordRequiresUpper);
        public const string PasswordRequiresUniqueChars = nameof(PasswordRequiresUniqueChars);
        public const string PasswordDenied = nameof(PasswordDenied);

        public const string EmailHostDenied = nameof(EmailHostDenied);
    }
}