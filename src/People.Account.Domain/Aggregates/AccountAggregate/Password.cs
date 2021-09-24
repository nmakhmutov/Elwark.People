using System;

namespace People.Account.Domain.Aggregates.AccountAggregate
{
    public sealed record Password(byte[] Hash, byte[] Salt, DateTime CreatedAt)
    {
        public const int MaxLength = 999;
    }
}
