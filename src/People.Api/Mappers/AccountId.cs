using People.Domain.Aggregates.AccountAggregate;

// ReSharper disable once CheckNamespace
namespace People.Grpc.Common;

public sealed partial class AccountIdValue
{
    private AccountIdValue(AccountId id) =>
        Value = (long)id;

    public static implicit operator AccountId(AccountIdValue value) =>
        new(value.Value);

    public static implicit operator AccountIdValue(AccountId value) =>
        new(value);
}
