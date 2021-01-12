namespace People.Domain.AggregateModels.Account
{
    public sealed record Name(string Nickname, string? FirstName = null, string? LastName = null);
}