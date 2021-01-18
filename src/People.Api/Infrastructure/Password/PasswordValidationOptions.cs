namespace People.Api.Infrastructure.Password
{
    public class PasswordValidationOptions
    {
        public bool RequireDigit { get; set; } = false;

        public int RequiredLength { get; set; } = 8;

        public int RequiredUniqueChars { get; set; } = 1;

        public bool RequireLowercase { get; set; } = false;

        public bool RequireNonAlphanumeric { get; set; } = false;

        public bool RequireUppercase { get; set; } = false;
    }
}