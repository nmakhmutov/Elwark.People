using Microsoft.EntityFrameworkCore;
using People.Domain.Events;

namespace People.Infrastructure.Outbox.EntityFrameworkCore;

public sealed class OutboxSaveChangesPipeline<TDbContext>
    where TDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IOutboxEventMapper<TDbContext>[] _mappers;
    private readonly List<IDomainEvent> _preparedDomainEvents = [];

    public OutboxSaveChangesPipeline(
        IDomainEventDispatcher dispatcher,
        IEnumerable<IOutboxEventMapper<TDbContext>> mappers
    )
    {
        _dispatcher = dispatcher;
        _mappers = mappers.ToArray();
    }

    public void Prepare(TDbContext dbContext)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries()
            .Select(static x => x.Entity)
            .OfType<IHasDomainEvents>()
            .Where(static x => x.GetDomainEvents().Count > 0)
            .ToArray();

        if (aggregates.Length == 0)
            return;

        var domainEvents = aggregates
            .SelectMany(x => x.GetDomainEvents())
            .ToArray();

        var messages = MapOutboxMessages(domainEvents).ToArray();
        if (messages.Length > 0)
            dbContext.Set<OutboxMessage>().AddRange(messages);

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        _preparedDomainEvents.AddRange(domainEvents);
    }

    public async ValueTask DispatchPreparedDomainEventsAsync(CancellationToken ct = default)
    {
        if (_preparedDomainEvents.Count == 0)
            return;

        var events = _preparedDomainEvents.ToArray();
        _preparedDomainEvents.Clear();
        await _dispatcher.DispatchAsync(events, ct);
    }

    private IEnumerable<OutboxMessage> MapOutboxMessages(IReadOnlyCollection<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            foreach (var mapper in _mappers)
            {
                if (mapper.Map(domainEvent) is { } message)
                    yield return message;
            }
        }
    }
}
