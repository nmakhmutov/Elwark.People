using System;
using System.Net.Mail;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class EmailIdentityModel : IdentityModel
    {
        public EmailIdentityModel(MailAddress address, EmailType type, DateTime? confirmedAt = null)
            : base(new EmailIdentity(address.ToString()), confirmedAt) =>
            EmailType = type;

        public EmailType EmailType { get; private set; }

        public override EmailIdentity GetIdentity() =>
            new(Value);
    }
}