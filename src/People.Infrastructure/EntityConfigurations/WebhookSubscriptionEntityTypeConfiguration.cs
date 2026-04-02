using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Application.Providers.Webhooks;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class WebhookSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.ToTable("webhooks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Type)
            .HasDatabaseName("IX_webhook_subscriptions_type");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Method)
            .HasColumnName("method")
            .IsRequired();

        builder.Property(x => x.DestinationUrl)
            .HasColumnName("destination_url")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Token)
            .HasColumnName("token")
            .HasMaxLength(256);
    }
}
