// ReSharper disable once CheckNamespace

namespace People.Grpc.Common;

public sealed partial class Name
{
    public static implicit operator Name(Domain.Aggregates.AccountAggregate.Name value) =>
        new()
        {
            Nickname = value.Nickname,
            FirstName = value.FirstName,
            LastName = value.LastName,
            FullName = value.FullName(),
            PreferNickname = value.PreferNickname
        };
}
