using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using People.Infrastructure.Outbox;

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

        var ctx = new PeopleDbContext(options, OutboxPipeline<PeopleDbContext>.Empty, TimeProvider.System);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
