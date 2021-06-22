using People.Domain.Aggregates.Account.Identities;

namespace People.Domain.Aggregates.Account
{
    public sealed record AccountEmail(EmailType Type, string Address, bool IsConfirmed)
    {
        public EmailIdentity GetIdentity() => new(Address);
    }
}