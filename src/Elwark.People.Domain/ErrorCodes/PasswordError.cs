namespace Elwark.People.Domain.ErrorCodes
{
    public enum PasswordError
    {
        Empty,
        TooShort,
        RequiresNonAlphanumeric,
        RequiresDigit,
        RequiresLower,
        RequiresUpper,
        RequiresUniqueChars,
        Mismatch,
        Worst,
        NotSet,
        AlreadySet
    }
}