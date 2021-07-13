using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.Account.Identities
{
    public class GoogleConnection : Connection

    {
        public GoogleConnection(string id, string? firstName, string? lastName, DateTime? confirmedAt)
            : base(IdentityType.Google, id, confirmedAt)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public override GoogleIdentity Identity => new(Value);
    }
}
