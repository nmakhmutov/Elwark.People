using System;
using System.Diagnostics;
using Elwark.People.Shared.Primitives;

namespace Elwark.People.Api.Application.Models
{
    public class BanDetail
    {
        [DebuggerStepThrough]
        public BanDetail(BanType type, DateTimeOffset createdAt, DateTimeOffset? expiredAt, string reason)
        {
            Reason = reason;
            Type = type;
            ExpiredAt = expiredAt;
            CreatedAt = createdAt;
        }

        public BanType Type { get; }
        
        public DateTimeOffset CreatedAt { get; }
        
        public string Reason { get; }

        public DateTimeOffset? ExpiredAt { get; }
    }
}