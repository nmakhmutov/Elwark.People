using System;

namespace People.Domain.Aggregates.AccountAggregate
{
    public sealed record Password(byte[] Hash, byte[] Salt, DateTime CreatedAt)
    {
        public const int MaxLength = 999;
    }
}
