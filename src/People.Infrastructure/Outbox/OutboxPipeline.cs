using Microsoft.EntityFrameworkCore;
using People.Domain.Events;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

public sealed class OutboxPipeline<TDbContext>
    where TDbContext : DbContext
{
    public static readonly OutboxPipeline<TDbContext> Empty =
        new(new OutboxMapperRegistry<TDbContext>());

    private readonly OutboxMapperRegistry<TDbContext> _registry;

    public OutboxPipeline(OutboxMapperRegistry<TDbContext> registry) =>
        _registry = registry;

    public void Prepare(DbContext dbContext)
    {
        var aggregates = dbContext.ChangeTracker
            .Entries()
            .Select(static x => x.Entity)
            .OfType<IHasDomainEvents>()
            .Where(static x => x.GetDomainEvents().Count > 0)
            .ToArray();

        if (aggregates.Length == 0)
            return;

        var messages = aggregates
            .SelectMany(static x => x.GetDomainEvents())
            .Select(x => _registry.Map(x))
            .OfType<OutboxMessage>()
            .ToArray();

        if (messages.Length > 0)
            dbContext.Set<OutboxMessage>().AddRange(messages);

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();
    }
}
