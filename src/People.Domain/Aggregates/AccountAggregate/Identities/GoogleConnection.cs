using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Identities
{
    public class GoogleConnection : Connection

    {
        public GoogleConnection(string id, string? firstName, string? lastName, DateTime? confirmedAt)
            : base(Type.Google, id, confirmedAt)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public override Identity.Google Identity => new(Value);
    }
}
