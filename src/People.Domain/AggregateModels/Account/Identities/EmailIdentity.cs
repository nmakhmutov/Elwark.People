using System;
using System.Net.Mail;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace People.Domain.AggregateModels.Account.Identities
{
    public sealed class EmailIdentity : Identity
    {
        public EmailIdentity(MailAddress address, EmailNotificationType notificationType, DateTime? confirmedAt)
            : base(new IdentityKey(IdentityType.Email, address.Address), confirmedAt)
        {
            NotificationType = notificationType;
        }

        public EmailNotificationType NotificationType { get; private set; }
    }
}