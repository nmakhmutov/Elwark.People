using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Application.Webhooks;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class WebhookConsumerEntityTypeConfiguration : IEntityTypeConfiguration<WebhookConsumer>
{
    public void Configure(EntityTypeBuilder<WebhookConsumer> builder)
    {
        builder.ToTable("webhook_consumers");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Type);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

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
