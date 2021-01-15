using System.Net.Mail;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class EmailIdentity : Identity
    {
        public EmailIdentity(MailAddress address, EmailType type)
            : base(IdentityKey.Email(address))=>
            EmailType = type;

        public EmailType EmailType { get; private set; }
    }
}