using Microsoft.EntityFrameworkCore;
using People.Infrastructure;

namespace People.IntegrationTests.Infrastructure;

internal static class IntegrationDatabaseCleanup
{
    /// <summary>Removes all application rows (no FK from confirmations to accounts in migrations).</summary>
    public static async Task DeleteAllAsync(PeopleDbContext ctx, CancellationToken ct = default)
    {
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM confirmations;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM emails;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM connections;", ct);
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM accounts;", ct);
    }
}
