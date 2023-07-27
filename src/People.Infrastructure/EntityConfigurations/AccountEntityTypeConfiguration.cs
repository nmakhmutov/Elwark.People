using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using People.Domain.Entities;
using People.Domain.ValueObjects;
using TimeZone = People.Domain.ValueObjects.TimeZone;

namespace People.Infrastructure.EntityConfigurations;

internal sealed class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(x => x.Id);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsBaned);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(x => (long)x, x => new AccountId(x))
            .UseIdentityByDefaultColumn();

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

        builder.Property(x => x.ContinentCode)
            .HasColumnName("continent_code")
            .HasConversion(x => x.ToString(), x => ContinentCode.Parse(x))
            .HasDefaultValue(ContinentCode.Empty)
            .HasMaxLength(2)
            .IsRequired();
            
        builder.Property(x => x.CountryCode)
            .HasColumnName("country_code")
            .HasConversion(x => x.ToString(), x => CountryCode.Parse(x))
            .HasDefaultValue(CountryCode.Empty)
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

        builder.Property<byte[]>("_regIp")
            .HasColumnName("reg_ip")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property<CountryCode>("_regCountryCode")
            .HasColumnName("reg_country_code")
            .HasConversion(x => x.ToString(), x => CountryCode.Parse(x))
            .HasMaxLength(2)
            .IsRequired();

        builder.Property<DateTime>("_lastActive")
            .HasColumnName("last_active")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property<DateTime>("_lastLogIn")
            .HasColumnName("last_log_in")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property<DateTime>("_updatedAt")
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property<DateTime>("_createdAt")
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .ValueGeneratedOnAdd()
            .IsRequired();

        builder.Property<uint>("version")
            .IsRowVersion();

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
