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
        var now = context.FireTimeUtc.UtcDateTime;
        
        await dbContext.Confirmations
            .Where(x => x.ExpiresAt < now)
            .ExecuteDeleteAsync(context.CancellationToken);
    }
}
