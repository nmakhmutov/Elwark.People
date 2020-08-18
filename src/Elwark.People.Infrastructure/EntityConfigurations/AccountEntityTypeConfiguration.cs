using System;
using System.Collections.Generic;
using System.Globalization;
using Elwark.People.Domain.AggregatesModel.AccountAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;

namespace Elwark.People.Infrastructure.EntityConfigurations
{
    internal class AccountEntityTypeConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("accounts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id");

            builder.Ignore(x => x.DomainEvents);

            builder.OwnsOne(x => x.Name, navigationBuilder =>
            {
                navigationBuilder.Ignore(x => x.FullName);
                
                navigationBuilder.Property(x => x.FirstName)
                    .HasColumnName("first_name")
                    .IsRequired(false)
                    .HasMaxLength(InfrastructureConstant.FirstNameLength);

                navigationBuilder.Property(x => x.LastName)
                    .HasColumnName("last_name")
                    .IsRequired(false)
                    .HasMaxLength(InfrastructureConstant.LastNameLength);

                navigationBuilder.Property(x => x.Nickname)
                    .HasColumnName("nickname")
                    .IsRequired()
                    .HasMaxLength(InfrastructureConstant.NicknameLength);
            });

            builder.OwnsOne(x => x.BasicInfo, navigationBuilder =>
            {
                navigationBuilder.Property(x => x.Birthday)
                    .HasColumnName("birthday")
                    .IsRequired(false)
                    .HasColumnType("date");

                navigationBuilder.Property(x => x.Gender)
                    .HasColumnName("gender")
                    .IsRequired()
                    .IsRequired();

                navigationBuilder.Property(x => x.Bio)
                    .HasColumnName("bio")
                    .IsRequired(false)
                    .HasMaxLength(InfrastructureConstant.BioLength);

                navigationBuilder.Property(x => x.Timezone)
                    .HasColumnName("timezone")
                    .IsRequired()
                    .HasMaxLength(InfrastructureConstant.TimezoneLength);

                navigationBuilder.Property(x => x.Language)
                    .HasColumnName("language")
                    .HasMaxLength(2)
                    .IsRequired()
                    .HasConversion(
                        info => info.TwoLetterISOLanguageName.ToLower(),
                        s => new CultureInfo(s)
                    );
            });

            builder.OwnsOne(x => x.Address, navigationBuilder =>
            {
                navigationBuilder.Property(x => x.CountryCode)
                    .HasColumnName("country_code")
                    .HasMaxLength(InfrastructureConstant.CountryCodeLength);

                navigationBuilder.Property(x => x.City)
                    .HasColumnName("city")
                    .HasMaxLength(InfrastructureConstant.CityLength);
            });

            builder.Property(x => x.Links)
                .HasColumnName("links")
                .HasConversion(
                    links => JsonConvert.SerializeObject(links),
                    json => JsonConvert.DeserializeObject<Links>(json)
                )
                .HasColumnType("json")
                .IsRequired();

            builder.OwnsOne(x => x.Password, navigationBuilder =>
            {
                navigationBuilder.WithOwner()
                    .HasForeignKey("AccountId");

                navigationBuilder.ToTable("passwords");

                navigationBuilder.HasKey("AccountId");

                navigationBuilder.Property<long>("AccountId")
                    .HasColumnName("account_id");

                navigationBuilder.Property(x => x!.Hash)
                    .HasColumnName("hash")
                    .IsRequired();

                navigationBuilder.Property(x => x!.Salt)
                    .HasColumnName("salt")
                    .IsRequired();

                navigationBuilder.Property<DateTimeOffset>("_createdAt")
                    .HasColumnName("created_at")
                    .IsCreatedAt();
            });

            builder.OwnsOne(x => x.Ban, ownershipBuilder =>
            {
                ownershipBuilder.WithOwner()
                    .HasForeignKey("AccountId");

                ownershipBuilder.ToTable("bans");

                ownershipBuilder.HasKey("AccountId");

                ownershipBuilder.Property<long>("AccountId")
                    .HasColumnName("account_id");

                ownershipBuilder.Property(x => x!.Type)
                    .HasColumnName("type")
                    .IsRequired();

                ownershipBuilder.Property(x => x!.CreatedAt)
                    .HasColumnName("created_at")
                    .IsRequired();

                ownershipBuilder.Property(x => x!.ExpiredAt)
                    .HasColumnName("expired_at")
                    .IsRequired(false);

                ownershipBuilder.Property(x => x!.Reason)
                    .HasColumnName("reason")
                    .HasMaxLength(InfrastructureConstant.BanReasonLength)
                    .IsRequired();
            });

            builder.Property(x => x.Picture)
                .HasColumnName("picture")
                .HasField("_picture")
                .IsUrl();

            builder.Property<List<string>>("_roles")
                .HasColumnName("roles")
                .IsRequired();

            builder.Property<DateTimeOffset>("_createdAt")
                .HasColumnName("created_at")
                .IsCreatedAt();

            builder.Property<DateTimeOffset>("_updatedAt")
                .HasColumnName("updated_at")
                .IsUpdatedAt();

            builder.Metadata
                .FindNavigation(nameof(Account.Identities))
                .SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}