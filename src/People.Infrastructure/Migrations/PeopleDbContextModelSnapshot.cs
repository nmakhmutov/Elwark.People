﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using People.Domain.AggregatesModel.AccountAggregate;
using People.Infrastructure;

#nullable disable

namespace People.Infrastructure.Migrations
{
    [DbContext(typeof(PeopleDbContext))]
    partial class PeopleDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.Account", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("CountryCode")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("character varying(2)")
                        .HasColumnName("country_code");

                    b.Property<string>("DateFormat")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("date_format");

                    b.Property<bool>("IsActivated")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("boolean")
                        .HasDefaultValue(false)
                        .HasColumnName("is_activated");

                    b.Property<string>("Language")
                        .IsRequired()
                        .HasMaxLength(2)
                        .HasColumnType("character varying(2)")
                        .HasColumnName("language");

                    b.Property<string>("Picture")
                        .IsRequired()
                        .HasMaxLength(2048)
                        .HasColumnType("character varying(2048)")
                        .HasColumnName("picture");

                    b.Property<int>("StartOfWeek")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasDefaultValue(1)
                        .HasColumnName("start_of_week");

                    b.Property<string>("TimeFormat")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("time_format");

                    b.Property<string>("TimeZone")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("character varying(128)")
                        .HasColumnName("time_zone");

                    b.Property<Ban>("_ban")
                        .HasColumnType("json")
                        .HasColumnName("ban");

                    b.Property<DateTime>("_createdAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.Property<string[]>("_roles")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("roles");

                    b.Property<DateTime>("_updatedAt")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("updated_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.ToTable("accounts", (string)null);
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.EmailAccount", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<long>("AccountId")
                        .HasColumnType("bigint")
                        .HasColumnName("account_id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("character varying(512)")
                        .HasColumnName("email");

                    b.Property<bool>("IsPrimary")
                        .HasColumnType("boolean")
                        .HasColumnName("is_primary");

                    b.Property<DateTime?>("_confirmedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("confirmed_at");

                    b.Property<DateTime>("_createdAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasAlternateKey("Email");

                    b.HasIndex("AccountId");

                    b.ToTable("emails", (string)null);
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.ExternalConnection", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<string>("FirstName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("first_name");

                    b.Property<string>("Identity")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("character varying(64)")
                        .HasColumnName("identity");

                    b.Property<string>("LastName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)")
                        .HasColumnName("last_name");

                    b.Property<byte>("Type")
                        .HasColumnType("smallint")
                        .HasColumnName("type");

                    b.Property<long>("_accountId")
                        .HasColumnType("bigint")
                        .HasColumnName("account_id");

                    b.Property<DateTime>("_createdAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("now()");

                    b.HasKey("Id");

                    b.HasAlternateKey("Type", "Identity");

                    b.HasIndex("_accountId");

                    b.ToTable("connections", (string)null);
                });

            modelBuilder.Entity("People.Infrastructure.Confirmations.Confirmation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid")
                        .HasColumnName("id")
                        .HasDefaultValueSql("gen_random_uuid()");

                    b.Property<long>("AccountId")
                        .HasColumnType("bigint")
                        .HasColumnName("account_id");

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(16)
                        .HasColumnType("character varying(16)")
                        .HasColumnName("code");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("created_at");

                    b.Property<DateTime>("ExpiresAt")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("expires_at");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("character varying(32)")
                        .HasColumnName("type");

                    b.HasKey("Id");

                    b.HasIndex("AccountId", "Type");

                    b.ToTable("confirmations", (string)null);
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.Account", b =>
                {
                    b.OwnsOne("People.Domain.AggregatesModel.AccountAggregate.Name", "Name", b1 =>
                        {
                            b1.Property<long>("AccountId")
                                .HasColumnType("bigint");

                            b1.Property<string>("FirstName")
                                .HasMaxLength(128)
                                .HasColumnType("character varying(128)")
                                .HasColumnName("first_name");

                            b1.Property<string>("LastName")
                                .HasMaxLength(128)
                                .HasColumnType("character varying(128)")
                                .HasColumnName("last_name");

                            b1.Property<string>("Nickname")
                                .IsRequired()
                                .HasMaxLength(64)
                                .HasColumnType("character varying(64)")
                                .HasColumnName("nickname");

                            b1.Property<bool>("PreferNickname")
                                .HasColumnType("boolean")
                                .HasColumnName("prefer_nickname");

                            b1.HasKey("AccountId");

                            b1.ToTable("accounts");

                            b1.WithOwner()
                                .HasForeignKey("AccountId");
                        });

                    b.OwnsOne("People.Domain.AggregatesModel.AccountAggregate.Registration", "_registration", b1 =>
                        {
                            b1.Property<long>("AccountId")
                                .HasColumnType("bigint");

                            b1.Property<string>("CountryCode")
                                .IsRequired()
                                .HasMaxLength(2)
                                .HasColumnType("character varying(2)")
                                .HasColumnName("reg_country_code");

                            b1.Property<byte[]>("Ip")
                                .IsRequired()
                                .HasColumnType("bytea")
                                .HasColumnName("reg_ip");

                            b1.HasKey("AccountId");

                            b1.ToTable("accounts");

                            b1.WithOwner()
                                .HasForeignKey("AccountId");
                        });

                    b.Navigation("Name")
                        .IsRequired();

                    b.Navigation("_registration");
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.EmailAccount", b =>
                {
                    b.HasOne("People.Domain.AggregatesModel.AccountAggregate.Account", null)
                        .WithMany("Emails")
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.ExternalConnection", b =>
                {
                    b.HasOne("People.Domain.AggregatesModel.AccountAggregate.Account", null)
                        .WithMany("Externals")
                        .HasForeignKey("_accountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("People.Domain.AggregatesModel.AccountAggregate.Account", b =>
                {
                    b.Navigation("Emails");

                    b.Navigation("Externals");
                });
#pragma warning restore 612, 618
        }
    }
}
