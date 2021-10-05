namespace People.Domain.Aggregates.AccountAggregate.Identities;

public enum IdentityType
{
    Email = 1,
    Google = 2,
    Microsoft = 3
}

public abstract record Identity(IdentityType Type, string Value)
{
    public record Email(string Value) : Identity(IdentityType.Email, Value.ToLowerInvariant());

    public record Google(string Value) : Identity(IdentityType.Google, Value);

    public record Microsoft(string Value) : Identity(IdentityType.Microsoft, Value);
}
