namespace People.Account.Api.Infrastructure.Password
{
    public sealed record PasswordResult(bool IsSuccess, string? Error)
    {
        public static PasswordResult Success() => new(true, null);
        
        public static PasswordResult Fail(string error) => new(false, error);
    }
}
