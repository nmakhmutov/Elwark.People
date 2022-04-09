using System;
using People.Domain.Aggregates.AccountAggregate.Identities;
using People.Domain.Seed;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
namespace People.Domain.Aggregates.AccountAggregate.Connections;

public abstract class Connection : ValueObject
{
    protected Connection(string value, DateTime createdAt)
    {
        Value = value;
        CreatedAt = createdAt;
    }

    public string Value { get; private set; }

    public DateTime? ConfirmedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool IsConfirmed =>
        ConfirmedAt.HasValue;

    public abstract Identity Identity { get; }

    internal void Confirm(DateTime confirmedAt) =>
        ConfirmedAt = confirmedAt;

    internal void Confute() =>
        ConfirmedAt = null;
}
