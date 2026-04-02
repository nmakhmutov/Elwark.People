using Microsoft.EntityFrameworkCore;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox.Extensions;

public static class OutboxQueryExtensions
{
    public static async Task<List<OutboxMessage>> GetPendingMessagesAsync(
        this IOutboxDbContext dbContext,
        DateTime utcNow,
        int batchSize,
        CancellationToken ct = default
    ) =>
        await dbContext.OutboxMessages
            .FromSqlInterpolated(
                $"""
                 SELECT * FROM outbox_messages
                 WHERE processed_at IS NULL
                   AND (next_retry_at IS NULL OR next_retry_at <= {utcNow})
                 ORDER BY occurred_at
                 LIMIT {batchSize}
                 FOR UPDATE SKIP LOCKED
                 """
            )
            .AsTracking()
            .ToListAsync(ct);

    public static async Task<int> CleanupOutboxAsync<TDbContext>(
        this TDbContext dbContext,
        DateTime utcNow,
        CancellationToken ct = default
    ) where TDbContext : DbContext, IOutboxDbContext
    {
        var completedCutoff = utcNow.AddDays(-7);
        var failedCutoff = utcNow.AddDays(-30);
        const int completedStatus = (int)OutboxStatus.Completed;
        const int failedStatus = (int)OutboxStatus.Failed;

        return await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             WITH deleted_messages AS (
                 DELETE FROM outbox_messages
                 WHERE (status = {completedStatus} AND occurred_at < {completedCutoff})
                    OR (status = {failedStatus} AND occurred_at < {failedCutoff})
                 RETURNING id
             )
             DELETE FROM outbox_consumers
             WHERE outbox_message_id IN (SELECT id FROM deleted_messages)
             """,
            ct
        );
    }
}
