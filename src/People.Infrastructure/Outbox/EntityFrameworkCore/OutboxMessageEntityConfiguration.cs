using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace People.Infrastructure.Outbox.EntityFrameworkCore;

public sealed class OutboxMessageEntityConfiguration(string tableName, string? schema)
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(tableName, schema);

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.ProcessedAt, x.OccurredAt })
            .HasFilter("processed_at IS NULL");

        builder.HasIndex(x => new { x.ProcessedAt, x.NextRetryAt, x.OccurredAt })
            .HasFilter("processed_at IS NULL");

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnOrder(0)
            .IsRequired();

        builder.Property<string>("_type")
            .HasColumnName("type")
            .HasColumnOrder(1)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property<string>("_payload")
            .HasColumnName("payload")
            .HasColumnOrder(2)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property<string>("_error")
            .HasColumnName("error")
            .HasColumnOrder(3)
            .HasMaxLength(OutboxMessage.MaxErrorLength);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnOrder(4)
            .IsRequired();

        builder.Property<int>("_attempts")
            .HasColumnName("attempts")
            .HasColumnOrder(5)
            .IsRequired();

        builder.Property(x => x.NextRetryAt)
            .HasColumnName("next_retry_at")
            .HasColumnOrder(6);

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnOrder(7);

        builder.Property(x => x.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnOrder(8)
            .IsRequired();
    }
}
