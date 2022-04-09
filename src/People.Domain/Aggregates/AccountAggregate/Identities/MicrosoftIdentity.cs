using System.Collections.Generic;

namespace People.Domain.Aggregates.AccountAggregate.Identities;

public sealed class MicrosoftIdentity : Identity
{
    public MicrosoftIdentity(string value)
        : base(value)
    {
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return nameof(MicrosoftIdentity);
        yield return Value;
    }
}
