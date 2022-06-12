using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Domain.AggregatesModel.AccountAggregate;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class EmailEntityTypeConfiguration : IEntityTypeConfiguration<EmailAccount>
{
    public void Configure(EntityTypeBuilder<EmailAccount> builder)
    {
        builder.ToTable("emails");

        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => x.Email);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsConfirmed);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AccountId)
            .HasColumnName("account_id")
            .IsRequired();

        builder.Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .HasColumnName("is_primary")
            .IsRequired();

        builder.Property<DateTime?>("_confirmedAt")
            .HasColumnName("confirmed_at");

        builder.Property<DateTime>("_createdAt")
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();
    }
}
