namespace People.Domain.Exceptions
{
    public static class ElwarkExceptionCodes
    {
        public const string Internal = nameof(Internal);
        
        public const string AccountNotFound = nameof(AccountNotFound);
        public const string AccountBanned = nameof(AccountBanned);

        public const string IdentityNotConfirmed = nameof(IdentityNotConfirmed);
        public const string IdentityAlreadyConfirmed = nameof(IdentityAlreadyConfirmed);
        
        public const string EmailAlreadyExists = nameof(EmailAlreadyExists);
        public const string EmailIncorrectFormat = nameof(EmailIncorrectFormat);
        public const string EmailHostDenied = nameof(EmailHostDenied);
        
        public const string PasswordEmpty = nameof(PasswordEmpty);
        public const string PasswordTooShort = nameof(PasswordTooShort);
        public const string PasswordRequiresNonAlphanumeric = nameof(PasswordRequiresNonAlphanumeric);
        public const string PasswordRequiresDigit = nameof(PasswordRequiresDigit);
        public const string PasswordRequiresLower = nameof(PasswordRequiresLower);
        public const string PasswordRequiresUpper = nameof(PasswordRequiresUpper);
        public const string PasswordRequiresUniqueChars = nameof(PasswordRequiresUniqueChars);
        public const string PasswordDenied = nameof(PasswordDenied);
        public const string PasswordNotCreated = nameof(PasswordNotCreated);
        public const string PasswordMismatch = nameof(PasswordMismatch);

        public const string ProviderUnauthorized = nameof(ProviderUnauthorized);
        public const string ProviderUnknown = nameof(ProviderUnknown);
        public const string ProviderAlreadyExists = nameof(ProviderAlreadyExists);

        public const string ConfirmationAlreadySent = nameof(ConfirmationAlreadySent);
        public const string ConfirmationNotMatch = nameof(ConfirmationNotMatch);
        public const string ConfirmationNotFound = nameof(ConfirmationNotFound);
    }
}