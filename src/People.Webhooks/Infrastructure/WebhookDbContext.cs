using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using People.Webhooks.Infrastructure.EntityConfigurations;
using People.Webhooks.Model;

namespace People.Webhooks.Infrastructure;

public sealed class WebhookDbContext : DbContext
{
    public WebhookDbContext(DbContextOptions<WebhookDbContext> options)
        : base(options)
    {
    }

    public DbSet<WebhookSubscription> Subscriptions =>
        Set<WebhookSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new WebhookSubscriptionEntityTypeConfiguration());
}

public class WebhookDbContextDesignFactory : IDesignTimeDbContextFactory<WebhookDbContext>
{
    public WebhookDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WebhookDbContext>()
            .UseNpgsql("Host=_;Database=_;Username=_;Password=_");

        return new WebhookDbContext(optionsBuilder.Options);
    }
}
