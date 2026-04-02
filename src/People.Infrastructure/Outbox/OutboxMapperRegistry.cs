using Microsoft.EntityFrameworkCore;
using People.Domain.Events;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

public sealed class OutboxMapperRegistry<TDbContext>
    where TDbContext : DbContext
{
    private readonly Dictionary<Type, Func<IDomainEvent, OutboxMessage>> _mappers = [];

    public OutboxMapperRegistry<TDbContext> AddMapper<TDomainEvent>(IOutboxEventMapper<TDomainEvent> mapper)
        where TDomainEvent : IDomainEvent
    {
        _mappers[typeof(TDomainEvent)] = evt => mapper.Map((TDomainEvent)evt);
        return this;
    }

    public OutboxMessage? Map(IDomainEvent domainEvent) =>
        _mappers.TryGetValue(domainEvent.GetType(), out var map)
            ? map(domainEvent)
            : null;
}
