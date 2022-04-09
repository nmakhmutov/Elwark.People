using System.Collections.Generic;

namespace People.Domain.Aggregates.AccountAggregate.Identities;

public sealed class GoogleIdentity : Identity
{
    public GoogleIdentity(string value)
        : base(value)
    {
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return nameof(GoogleIdentity);
        yield return Value;
    }
}
