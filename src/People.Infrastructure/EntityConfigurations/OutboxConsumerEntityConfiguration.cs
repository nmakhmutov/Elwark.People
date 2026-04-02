using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Infrastructure.Outbox.Entities;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class OutboxConsumerEntityConfiguration : IEntityTypeConfiguration<OutboxConsumer>
{
    public void Configure(EntityTypeBuilder<OutboxConsumer> builder)
    {
        builder.ToTable("outbox_consumers");

        builder.HasKey(x => new { x.MessageId, x.Consumer });

        builder.Property(x => x.MessageId)
            .HasColumnName("message_id")
            .HasColumnOrder(0)
            .IsRequired();

        builder.Property(x => x.Consumer)
            .HasColumnName("consumer")
            .HasColumnOrder(1)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnOrder(2)
            .IsRequired();
    }
}
