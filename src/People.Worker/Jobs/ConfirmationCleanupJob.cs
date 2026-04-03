using Microsoft.EntityFrameworkCore;
using People.Infrastructure;
using Quartz;

namespace People.Worker.Jobs;

[DisallowConcurrentExecution]
public sealed class ConfirmationCleanupJob(IDbContextFactory<PeopleDbContext> factory) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        await using var dbContext = await factory.CreateDbContextAsync();

        await dbContext.Confirmations
            .Where(x => x.ExpiresAt < context.FireTimeUtc.UtcDateTime)
            .ExecuteDeleteAsync(context.CancellationToken);
    }
}
