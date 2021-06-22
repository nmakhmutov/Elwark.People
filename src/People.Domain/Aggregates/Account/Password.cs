using System;

namespace People.Domain.Aggregates.Account
{
    public sealed record Password(byte[] Hash, byte[] Salt, DateTime CreatedAt)
    {
        public const int MaxLength = 999;
    }
}
