using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using People.Infrastructure.Outbox.Extensions;
using Quartz;

namespace People.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed class OutboxCleanupJob(IDbContextFactory<PeopleDbContext> factory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        await dbContext.CleanupOutboxAsync(context.FireTimeUtc.UtcDateTime, context.CancellationToken);
    }
}
