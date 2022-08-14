using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Infrastructure.Confirmations;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class ConfirmationEntityTypeConfiguration : IEntityTypeConfiguration<Confirmation>
{
    public void Configure(EntityTypeBuilder<Confirmation> builder)
    {
        builder.ToTable("confirmations");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.AccountId, x.Type });

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
    }
}
