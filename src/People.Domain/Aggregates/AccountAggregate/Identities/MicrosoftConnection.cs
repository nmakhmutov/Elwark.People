using System;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Identities;

public sealed record MicrosoftConnection : Connection
{
    public MicrosoftConnection(string id, string? firstName, string? lastName, DateTime createdAt,
        DateTime? confirmedAt = null)
        : base(IdentityType.Microsoft, id, createdAt, confirmedAt)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public override Microsoft Identity => new(Value);
}
