using System.Collections.Generic;

namespace People.Domain.Aggregates.AccountAggregate.Identities;

public sealed class EmailIdentity : Identity
{
    public EmailIdentity(string value)
        : base(value.ToLowerInvariant())
    {
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return nameof(EmailIdentity);
        yield return Value;
    }
}
