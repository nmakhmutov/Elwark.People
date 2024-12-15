using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Domain.Entities;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class ExternalConnectionEntityTypeConfiguration : IEntityTypeConfiguration<ExternalConnection>
{
    public void Configure(EntityTypeBuilder<ExternalConnection> builder)
    {
        builder.ToTable("connections");

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new
        {
            Name = x.Type,
            x.Identity
        });

        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property<AccountId>("_accountId")
            .HasColumnName("account_id")
            .HasConversion(x => (long)x, x => new AccountId(x))
            .IsRequired();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .IsRequired();

        builder.Property(x => x.Identity)
            .HasColumnName("identity")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(256);

        builder.Property(x => x.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(256);

        builder.Property<DateTime>("_createdAt")
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();
    }
}
