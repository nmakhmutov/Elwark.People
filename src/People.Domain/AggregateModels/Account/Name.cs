namespace People.Domain.AggregateModels.Account
{
    public sealed record Name(string Nickname, string? FirstName = null, string? LastName = null)
    {
        public string FullName()
        {
            var fullName = $"{FirstName} {LastName}".Trim();
            return string.IsNullOrEmpty(fullName) ? Nickname : fullName;
        }
    }
}