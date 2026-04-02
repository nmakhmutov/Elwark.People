using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using People.Domain.Events;
using People.Infrastructure;
using People.Infrastructure.Outbox.EntityFrameworkCore;

namespace People.UnitTests.Infrastructure;

internal static class SqlitePeopleDbContextFactory
{
    /// <summary>SQLite in-memory <see cref="PeopleDbContext"/> for command handler tests that call <c>ExecuteUpdateAsync</c>.</summary>
    internal static PeopleDbContext CreateEmpty()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseSqlite(connection)
            .Options;

        var dispatcher = new NoOpDomainEventDispatcher();
        var pipeline = new OutboxSaveChangesPipeline<PeopleDbContext>(dispatcher, []);
        var ctx = new PeopleDbContext(options, pipeline, TimeProvider.System);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public ValueTask DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct = default) =>
            ValueTask.CompletedTask;
    }
}
