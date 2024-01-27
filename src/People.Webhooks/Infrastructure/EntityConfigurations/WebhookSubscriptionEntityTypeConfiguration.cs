using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Webhooks.Model;

namespace People.Webhooks.Infrastructure.EntityConfigurations;

public class WebhookSubscriptionEntityTypeConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook_subscriptions");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Type);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.Type)
            .HasColumnName("type")
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
