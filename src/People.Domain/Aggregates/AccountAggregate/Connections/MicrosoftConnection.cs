using System;
using System.Collections.Generic;
using People.Domain.Aggregates.AccountAggregate.Identities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Connections;

public sealed class MicrosoftConnection : Connection
{
    public MicrosoftConnection(MicrosoftIdentity microsoft, string? firstName, string? lastName, DateTime createdAt)
        : base(microsoft.Value, createdAt)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public override MicrosoftIdentity Identity => 
        new(Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Identity;
        yield return ConfirmedAt;
        yield return CreatedAt;
        yield return IsConfirmed;
        yield return FirstName;
        yield return LastName;
    }
}
