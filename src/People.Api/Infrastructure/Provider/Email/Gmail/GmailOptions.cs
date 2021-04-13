namespace People.Api.Infrastructure.Provider.Email.Gmail
{
    public sealed record GmailOptions
    {
        public string Username { get; init; } = string.Empty;

        public string Password { get; init; } = string.Empty;
    }
}
