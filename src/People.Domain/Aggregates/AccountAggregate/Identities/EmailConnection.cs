using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Identities
{
    public sealed class EmailConnection : Connection
    {
        public EmailConnection(string email, EmailType type, DateTime? confirmedAt = null)
            : base(Type.Email, email.ToLowerInvariant(), confirmedAt) =>
            EmailType = type;

        public EmailType EmailType { get; private set; }

        public override Identity.Email Identity => new(Value);

        public void ChangeType(EmailType type) =>
            EmailType = type;
    }
}
