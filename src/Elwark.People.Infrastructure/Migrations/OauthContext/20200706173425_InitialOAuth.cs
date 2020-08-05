using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Elwark.People.Infrastructure.Migrations.OauthContext
{
    public partial class InitialOAuth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                "accounts",
                table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(maxLength: 256, nullable: true),
                    last_name = table.Column<string>(maxLength: 256, nullable: true),
                    nickname = table.Column<string>(maxLength: 256, nullable: true),
                    country_code = table.Column<string>(maxLength: 2, nullable: true),
                    city = table.Column<string>(maxLength: 128, nullable: true),
                    links = table.Column<string>("json", nullable: false),
                    gender = table.Column<byte>(nullable: true),
                    language = table.Column<string>(maxLength: 2, nullable: true),
                    timezone = table.Column<string>(maxLength: 64, nullable: true),
                    birthday = table.Column<DateTime>("date", nullable: true),
                    bio = table.Column<string>(maxLength: 1024, nullable: true),
                    picture = table.Column<string>(maxLength: 2048, nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false,
                        defaultValueSql: "timezone('utc'::text, now())"),
                    roles = table.Column<List<string>>(nullable: false),
                    updated_at = table.Column<DateTimeOffset>(nullable: false,
                        defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table => { table.PrimaryKey("PK_accounts", x => x.id); });

            migrationBuilder.CreateTable(
                "confirmations",
                table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    identity_id = table.Column<Guid>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    code = table.Column<long>(nullable: false),
                    expired_at = table.Column<DateTimeOffset>("timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_confirmations", x => x.id);
                    table.UniqueConstraint("AK_confirmations_identity_id_code", x => new {x.identity_id, x.code});
                });

            migrationBuilder.CreateTable(
                "bans",
                table => new
                {
                    account_id = table.Column<long>(nullable: false),
                    type = table.Column<int>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false),
                    expired_at = table.Column<DateTimeOffset>(nullable: true),
                    reason = table.Column<string>(maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bans", x => x.account_id);
                    table.ForeignKey(
                        "FK_bans_accounts_account_id",
                        x => x.account_id,
                        "accounts",
                        "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "identities",
                table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    identification_type = table.Column<int>(nullable: false),
                    notification_type = table.Column<int>(nullable: false),
                    value = table.Column<string>(maxLength: 256, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(nullable: true),
                    account_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false,
                        defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identities", x => x.id);
                    table.ForeignKey(
                        "FK_identities_accounts_account_id",
                        x => x.account_id,
                        "accounts",
                        "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                "passwords",
                table => new
                {
                    account_id = table.Column<long>(nullable: false),
                    hash = table.Column<byte[]>(nullable: false),
                    salt = table.Column<byte[]>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false,
                        defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passwords", x => x.account_id);
                    table.ForeignKey(
                        "FK_passwords_accounts_account_id",
                        x => x.account_id,
                        "accounts",
                        "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_identities_account_id",
                "identities",
                "account_id");

            migrationBuilder.CreateIndex(
                "IX_identities_identification_type_value",
                "identities",
                new[] {"identification_type", "value"},
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "bans");

            migrationBuilder.DropTable(
                "confirmations");

            migrationBuilder.DropTable(
                "identities");

            migrationBuilder.DropTable(
                "passwords");

            migrationBuilder.DropTable(
                "accounts");
        }
    }
}