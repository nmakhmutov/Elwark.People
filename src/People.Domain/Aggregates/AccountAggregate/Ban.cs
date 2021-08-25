using System;

namespace People.Domain.Aggregates.AccountAggregate
{
    public abstract record Ban(string Reason, DateTime CreatedAt);
    
    public sealed record PermanentBan(string Reason, DateTime CreatedAt)
        : Ban(Reason, CreatedAt);

    public sealed record TemporaryBan(string Reason, DateTime CreatedAt, DateTime ExpiredAt)
        : Ban(Reason, CreatedAt);
}