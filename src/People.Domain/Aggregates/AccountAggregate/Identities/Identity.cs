using People.Domain.Seed;

namespace People.Domain.Aggregates.AccountAggregate.Identities;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
public abstract class Identity : ValueObject
{
    protected Identity(string value) =>
        Value = value;

    public string Value { get; private set; }
}
