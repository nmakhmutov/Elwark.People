using System;
using System.Net.Mail;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace People.Domain.AggregateModels.Account.Identities
{
    public abstract class Identity
    {
        protected Identity(IdentityKey key, DateTime? confirmedAt)
        {
            Key = key;
            ConfirmedAt = confirmedAt;
            CreatedAt = DateTime.UtcNow;
        }

        public IdentityKey Key { get; private set; }

        public DateTime? ConfirmedAt { get; protected set; }

        public DateTime CreatedAt { get; private set; }
    }

    public sealed record IdentityKey(IdentityType Type, string Value)
    {
        public static IdentityKey Email(MailAddress email) => 
            new (IdentityType.Email, email.Address);

        public static IdentityKey Google(string id) =>
            new(IdentityType.Google, id);

        public static IdentityKey Facebook(string id) =>
            new(IdentityType.Facebook, id);

        public static IdentityKey Microsoft(string id) =>
            new(IdentityType.Microsoft, id);
    }
}