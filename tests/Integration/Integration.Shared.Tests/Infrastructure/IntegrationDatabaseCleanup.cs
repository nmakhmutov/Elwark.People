using Microsoft.EntityFrameworkCore;
using People.Infrastructure;

namespace Integration.Shared.Tests.Infrastructure;

public static class IntegrationDatabaseCleanup
{
    /// <summary>Removes all application rows across both schemas in one PostgreSQL truncate.</summary>
    public static Task DeleteAllAsync(PeopleDbContext ctx, CancellationToken ct = default)
    {
        const string sql =
            """
            TRUNCATE TABLE
                confirmations,
                emails,
                connections,
                accounts,
                outbox_consumers,
                outbox_messages,
                webhook_messages,
                webhook_consumers
            RESTART IDENTITY
            CASCADE;
            """;

        return ctx.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
