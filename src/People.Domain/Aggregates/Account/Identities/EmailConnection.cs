using System;
using System.Net.Mail;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.Account.Identities
{
    public sealed class EmailConnection : Connection
    {
        public EmailConnection(MailAddress address, EmailType type, DateTime? confirmedAt = null)
            : base(IdentityType.Email, address.ToString().ToLowerInvariant(), confirmedAt) =>
            EmailType = type;

        public EmailType EmailType { get; private set; }

        public override EmailIdentity Identity => new(Value);

        public void ChangeType(EmailType type) =>
            EmailType = type;
    }
}
