using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Domain.Entities;
using People.Domain.SeedWork;
using People.Infrastructure.Confirmations;
using People.Infrastructure.EntityConfigurations;
using People.Infrastructure.Outbox;

namespace People.Infrastructure;

public sealed class PeopleDbContext : OutboxDbContext<PeopleDbContext>, IUnitOfWork
{
    private readonly TimeProvider _timeProvider;

    public DbSet<Account> Accounts =>
        Set<Account>();

    public DbSet<EmailAccount> Emails =>
        Set<EmailAccount>();

    public DbSet<ExternalConnection> Connections =>
        Set<ExternalConnection>();

    public DbSet<Confirmation> Confirmations =>
        Set<Confirmation>();

    public PeopleDbContext(
        DbContextOptions<PeopleDbContext> options,
        OutboxPipeline<PeopleDbContext> pipeline,
        TimeProvider timeProvider
    ) : base(options, pipeline) =>
        _timeProvider = timeProvider;

    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);

        return true;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.SetAsUpdated(_timeProvider);
        }

        return await base.SaveChangesAsync(ct);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ConfirmationEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new EmailEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ExternalConnectionEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxConsumerEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class OrderingContextDesignFactory : IDesignTimeDbContextFactory<PeopleDbContext>
{
    public PeopleDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PeopleDbContext>()
            .UseNpgsql(
                "Host=_;Database=_;Username=_;Password=_",
                npgsql => npgsql.ConfigureDataSource(x => x.EnableDynamicJson())
            );

        var pipeline = OutboxPipeline<PeopleDbContext>.Empty;
        return new PeopleDbContext(optionsBuilder.Options, pipeline, TimeProvider.System);
    }
}
