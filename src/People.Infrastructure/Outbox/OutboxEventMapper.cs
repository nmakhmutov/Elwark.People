using Microsoft.EntityFrameworkCore;
using People.Domain.Events;

namespace People.Infrastructure.Outbox;

public abstract class OutboxEventMapper<TDbContext, TDomainEvent> : IOutboxEventMapper<TDbContext>
    where TDbContext : DbContext
    where TDomainEvent : IDomainEvent
{
    OutboxMessage? IOutboxEventMapper<TDbContext>.Map(IDomainEvent evt) =>
        evt is TDomainEvent x ? Map(x) : null;

    public abstract OutboxMessage? Map(TDomainEvent evt);
}
