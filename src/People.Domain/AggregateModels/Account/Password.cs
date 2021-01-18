using System;

namespace People.Domain.AggregateModels.Account
{
    public sealed record Password(byte[] Hash, byte[] Salt, DateTime CreatedAt);
}