using Microsoft.EntityFrameworkCore;
using People.Infrastructure.EntityConfigurations;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.Outbox;

public abstract class OutboxDbContext<T> : DbContext, IOutboxDbContext where T : DbContext
{
    private readonly OutboxPipeline<T> _pipeline;

    protected OutboxDbContext(DbContextOptions<T> options, OutboxPipeline<T> pipeline) : base(options) =>
        _pipeline = pipeline;

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    public DbSet<OutboxConsumer> OutboxConsumers =>
        Set<OutboxConsumer>();

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        _pipeline.Prepare(this);
        return base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConsumerEntityConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
