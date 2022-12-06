using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Domain.AggregatesModel.AccountAggregate;
using TimeZone = People.Domain.AggregatesModel.AccountAggregate.TimeZone;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.Roles);
        builder.Ignore(x => x.IsBaned);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.OwnsOne(x => x.Name, navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Nickname)
                .HasColumnName("nickname")
                .HasMaxLength(Name.NicknameLength)
                .IsRequired();

            navigationBuilder.Property(x => x.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(Name.FirstNameLength);

            navigationBuilder.Property(x => x.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(Name.LastNameLength);

            navigationBuilder.Property(x => x.PreferNickname)
                .HasColumnName("prefer_nickname")
                .IsRequired();

            navigationBuilder.WithOwner();
        });

        builder.Property(x => x.Picture)
            .HasColumnName("picture")
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(x => x.Language)
            .HasColumnName("language")
            .HasConversion(x => x.ToString(), x => Language.Parse(x))
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasConversion(x => x.ToString(), x => CountryCode.Parse(x))
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.TimeZone)
            .HasColumnName("time_zone")
            .HasConversion(x => x.ToString(), x => TimeZone.Parse(x))
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.DateFormat)
            .HasColumnName("date_format")
            .HasConversion(x => x.ToString(), x => DateFormat.Parse(x))
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.TimeFormat)
            .HasColumnName("time_format")
            .HasConversion(x => x.ToString(), x => TimeFormat.Parse(x))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.StartOfWeek)
            .HasColumnName("start_of_week")
            .HasDefaultValue(DayOfWeek.Monday)
            .IsRequired();

        builder.Property(x => x.IsActivated)
            .HasColumnName("is_activated")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property("_roles")
            .HasColumnName("roles")
            .IsRequired();
        
        builder.Property<Ban>("_ban")
            .HasColumnName("ban")
            .HasColumnType("json");
        
        builder.OwnsOne<Registration>("_registration", navigationBuilder =>
        {
            navigationBuilder.Property(x => x.Ip)
                .HasColumnName("reg_ip")
                .HasColumnType("bytea")
                .IsRequired();

            navigationBuilder.Property(x => x.CountryCode)
                .HasColumnName("reg_country_code")
                .HasConversion(x => x.ToString(), x => CountryCode.Parse(x))
                .HasMaxLength(2)
                .IsRequired();

            navigationBuilder.WithOwner();
        });
        
        builder.Property<DateTime>("_updatedAt")
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property<DateTime>("_createdAt")
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.HasMany(x => x.Emails)
            .WithOne()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(x => x.Externals)
            .WithOne()
            .HasForeignKey("_accountId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
