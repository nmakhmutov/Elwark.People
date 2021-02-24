using System.Net.Mail;

namespace People.Domain.AggregateModels.Account.Identities
{
    public abstract record Identity(IdentityType Type, string Value);

    public sealed record EmailIdentity : Identity
    {
        public EmailIdentity(string email)
            : this(new MailAddress(email))
        {
        }

        public EmailIdentity(MailAddress address)
            : base(IdentityType.Email, address.ToString().ToLowerInvariant())
        {
        }

        public MailAddress GetMailAddress() => new(Value);
    }

    public sealed record GoogleIdentity : Identity
    {
        public GoogleIdentity(string id)
            : base(IdentityType.Google, id)
        {
        }
    }

    public sealed record MicrosoftIdentity : Identity
    {
        public MicrosoftIdentity(string id)
            : base(IdentityType.Microsoft, id)
        {
        }
    }
}