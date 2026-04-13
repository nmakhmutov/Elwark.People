using Microsoft.EntityFrameworkCore;
using People.Infrastructure.EntityConfigurations;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

public abstract class OutboxDbContext<T> : DbContext, IOutboxDbContext where T : DbContext
{
    private readonly OutboxPipeline<T> _pipeline;

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<OutboxConsumer> OutboxConsumers =>
        Set<OutboxConsumer>();

    protected OutboxDbContext(DbContextOptions<T> options, OutboxPipeline<T> pipeline) : base(options) =>
        _pipeline = pipeline;

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        if (Database.CurrentTransaction is not null)
            return await SaveWithPipelineAsync(ct);

        await using var tx = await Database.BeginTransactionAsync(ct);
        var result = await SaveWithPipelineAsync(ct);
        await tx.CommitAsync(ct);

        return result;
    }

    private async Task<int> SaveWithPipelineAsync(CancellationToken ct)
    {
        var result = await base.SaveChangesAsync(ct);
        _pipeline.Prepare(this);

        if (ChangeTracker.HasChanges())
            return await base.SaveChangesAsync(ct);

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConsumerEntityConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
