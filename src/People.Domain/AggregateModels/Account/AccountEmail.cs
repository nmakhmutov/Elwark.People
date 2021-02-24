using People.Domain.AggregateModels.Account.Identities;

namespace People.Domain.AggregateModels.Account
{
    public sealed record AccountEmail(EmailType Type, string Address, bool IsConfirmed)
    {
        public EmailIdentity GetIdentity() => new(Address);
    }
}