using System;
using System.Net.Mail;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.Account.Identities
{
    public sealed class EmailIdentityModel : IdentityModel
    {
        public EmailIdentityModel(MailAddress address, EmailType type, DateTime? confirmedAt = null)
            : base(new EmailIdentity(address), confirmedAt) =>
            EmailType = type;

        public EmailType EmailType { get; private set; }

        public override EmailIdentity GetIdentity() =>
            new(Value);

        public void ChangeType(EmailType type) =>
            EmailType = type;
    }
}