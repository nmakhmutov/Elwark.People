using System;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elwark.People.Infrastructure.EntityConfigurations
{
    public class IdentityEntityTypeConfiguration
        : IEntityTypeConfiguration<Identity>
    {
        public void Configure(EntityTypeBuilder<Identity> builder)
        {
            builder.ToTable("identities");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new {x.NotificationType, x.AccountId});

            builder.HasIndex(x => new {IdentifierType = x.IdentificationType, x.Value})
                .IsUnique();

            builder
                .Ignore(x => x.DomainEvents)
                .Ignore(x => x.Identification)
                .Ignore(x => x.Notification)
                .Ignore(x => x.IsConfirmed)
                .Ignore(x => x.AccountId);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            builder.Property<long>("_accountId")
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.IdentificationType)
                .HasColumnName("identification_type")
                .IsRequired();

            builder.Property(x => x.NotificationType)
                .HasColumnName("notification_type")
                .IsRequired();

            builder.Property(x => x.Value)
                .HasColumnName("value")
                .HasMaxLength(InfrastructureConstant.IdentityLength)
                .IsRequired();

            builder.Property(x => x.ConfirmedAt)
                .HasColumnName("confirmed_at");

            builder.Property<DateTimeOffset>("_createdAt")
                .HasColumnName("created_at")
                .IsCreatedAt();

            builder.HasOne<Account>()
                .WithMany(x => x.Identities)
                .HasForeignKey("_accountId")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}