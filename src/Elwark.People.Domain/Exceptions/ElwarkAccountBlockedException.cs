using System;
using Elwark.People.Abstractions;
using Elwark.People.Domain.ErrorCodes;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Domain.Exceptions
{
    public class ElwarkAccountBlockedException : ElwarkAccountException
    {
        public ElwarkAccountBlockedException(AccountId accountId, BanType type, DateTimeOffset? expiredAt,
            string reason)
            : base(AccountError.Blocked, accountId)
        {
            BanType = type;
            ExpiredAt = expiredAt;
            Reason = reason;
        }

        public BanType BanType { get; }

        public DateTimeOffset? ExpiredAt { get; }

        public string Reason { get; }
    }
}