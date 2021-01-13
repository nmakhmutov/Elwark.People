using System.Net.Mail;

namespace People.Domain.AggregateModels.Account.Identities
{
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