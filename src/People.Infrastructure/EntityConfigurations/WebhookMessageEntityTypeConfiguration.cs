using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Infrastructure.Webhooks;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class WebhookMessageEntityTypeConfiguration : IEntityTypeConfiguration<WebhookMessage>
{
    public void Configure(EntityTypeBuilder<WebhookMessage> builder)
    {
        builder.ToTable("webhook_messages");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.Status, x.RetryAfter });

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(x => x.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.RetryAfter)
            .HasColumnName("retry_after");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .IsRequired();
    }
}
