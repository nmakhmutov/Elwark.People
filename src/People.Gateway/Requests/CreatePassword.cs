namespace People.Gateway.Requests
{
    public sealed record CreatePassword(string Id, uint Code, string Password);
}