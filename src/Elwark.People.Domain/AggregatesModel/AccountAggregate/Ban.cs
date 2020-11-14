using System;
using System.Collections.Generic;
using Elwark.People.Domain.SeedWork;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Domain.AggregatesModel.AccountAggregate
{
    public record Ban : ValueObject
    {
        public Ban(BanType type, DateTimeOffset createdAt, DateTimeOffset? expiredAt, string reason)
        {
            Type = type;
            Reason = reason ?? throw new ArgumentNullException(nameof(reason));
            ExpiredAt = expiredAt;
            CreatedAt = createdAt;
        }

        public BanType Type { get; }

        public DateTimeOffset CreatedAt { get; }

        public DateTimeOffset? ExpiredAt { get; }

        public string Reason { get; } = string.Empty;

        protected override IEnumerable<object?> GetAtomicValues()
        {
            yield return Type;
            yield return CreatedAt;
            yield return ExpiredAt;
            yield return Reason;
        }
    }
}