using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Application.Webhooks;
using People.Infrastructure.EntityConfigurations;
using People.Infrastructure.Webhooks;

namespace People.Infrastructure;

public sealed class WebhookDbContext : DbContext
{
    public DbSet<WebhookConsumer> Consumers =>
        Set<WebhookConsumer>();

    public DbSet<WebhookMessage> Messages =>
        Set<WebhookMessage>();

    public WebhookDbContext(DbContextOptions<WebhookDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new WebhookConsumerEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new WebhookMessageEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}

public sealed class WebhookContextDesignFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseNpgsql("Host=_;Database=_;Username=_;Password=_");

        return new WebhookDbContext(optionsBuilder.Options);
    }
}
