using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Identities
{
    public sealed class MicrosoftConnection : Connection
    {
        public MicrosoftConnection(string id, string? firstName, string? lastName, DateTime? confirmedAt)
            : base(Type.Microsoft, id, confirmedAt)
        {
            FirstName = firstName;
            LastName = lastName;
        }

        public string? FirstName { get; private set; }

        public string? LastName { get; private set; }

        public override Identity.Microsoft Identity => new(Value);
    }
}
