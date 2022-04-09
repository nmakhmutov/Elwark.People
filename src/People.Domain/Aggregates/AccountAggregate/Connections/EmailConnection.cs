using System;
using System.Collections.Generic;
using People.Domain.Aggregates.AccountAggregate.Identities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Connections;

public sealed class EmailConnection : Connection
{
    public EmailConnection(EmailIdentity email, DateTime createdAt)
        : base(email.Value, createdAt)
    {
    }
    
    public bool IsPrimary { get; private set; }

    public override EmailIdentity Identity =>
        new(Value);

    internal bool SetPrimary() =>
        IsPrimary = true;

    internal bool RemovePrimary() =>
        IsPrimary = false;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Identity;
        yield return IsPrimary;
        yield return ConfirmedAt;
        yield return CreatedAt;
        yield return IsConfirmed;
    }
}
