using Elwark.People.Abstractions;
using Elwark.People.Infrastructure.Confirmation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Elwark.People.Infrastructure.EntityConfigurations
{
    public class ConfirmationEntityTypeConfiguration : IEntityTypeConfiguration<ConfirmationModel>
    {
        public void Configure(EntityTypeBuilder<ConfirmationModel> builder)
        {
            builder.ToTable("confirmations");

            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new {x.IdentityId, x.Code});

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("uuid_generate_v4()");

            builder.Property(x => x.Code)
                .HasColumnName("code")
                .IsRequired();

            builder.Property(x => x.IdentityId)
                .HasColumnName("identity_id")
                .HasConversion(id => id.Value, guid => new IdentityId(guid))
                .IsRequired();

            builder.Property(x => x.Type)
                .HasColumnName("type")
                .IsRequired();

            builder.Property(x => x.ExpiredAt)
                .HasColumnName("expired_at")
                .HasColumnType("timestamptz")
                .IsRequired();
        }
    }
}