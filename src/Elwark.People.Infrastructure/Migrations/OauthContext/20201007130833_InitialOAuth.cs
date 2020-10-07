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
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(maxLength: 256, nullable: true),
                    last_name = table.Column<string>(maxLength: 256, nullable: true),
                    nickname = table.Column<string>(maxLength: 256, nullable: true),
                    country_code = table.Column<string>(maxLength: 2, nullable: true),
                    city = table.Column<string>(maxLength: 128, nullable: true),
                    links = table.Column<string>(type: "json", nullable: false),
                    gender = table.Column<byte>(nullable: true),
                    language = table.Column<string>(maxLength: 2, nullable: true),
                    timezone = table.Column<string>(maxLength: 64, nullable: true),
                    birthday = table.Column<DateTime>(type: "date", nullable: true),
                    bio = table.Column<string>(maxLength: 1024, nullable: true),
                    picture = table.Column<string>(maxLength: 2048, nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    roles = table.Column<List<string>>(nullable: false),
                    updated_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bans",
                columns: table => new
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
                        name: "FK_bans_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identities",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    identification_type = table.Column<int>(nullable: false),
                    notification_type = table.Column<int>(nullable: false),
                    value = table.Column<string>(maxLength: 256, nullable: false),
                    confirmed_at = table.Column<DateTimeOffset>(nullable: true),
                    account_id = table.Column<long>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_identities", x => x.id);
                    table.ForeignKey(
                        name: "FK_identities_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "passwords",
                columns: table => new
                {
                    account_id = table.Column<long>(nullable: false),
                    hash = table.Column<byte[]>(nullable: false),
                    salt = table.Column<byte[]>(nullable: false),
                    created_at = table.Column<DateTimeOffset>(nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passwords", x => x.account_id);
                    table.ForeignKey(
                        name: "FK_passwords_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_identities_account_id",
                table: "identities",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_identities_identification_type_value",
                table: "identities",
                columns: new[] { "identification_type", "value" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bans");

            migrationBuilder.DropTable(
                name: "identities");

            migrationBuilder.DropTable(
                name: "passwords");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
