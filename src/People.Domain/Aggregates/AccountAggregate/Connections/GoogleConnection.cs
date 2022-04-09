using System;
using System.Collections.Generic;
using People.Domain.Aggregates.AccountAggregate.Identities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Connections;

public sealed class GoogleConnection : Connection
{
    public GoogleConnection(GoogleIdentity google, string? firstName, string? lastName, DateTime createdAt)
        : base(google.Value, createdAt)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public override GoogleIdentity Identity => 
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
