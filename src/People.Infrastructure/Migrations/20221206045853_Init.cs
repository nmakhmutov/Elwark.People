using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using People.Domain.ValueObjects;

#nullable disable

namespace People.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nickname = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    firstname = table.Column<string>(name: "first_name", type: "character varying(128)", maxLength: 128, nullable: true),
                    lastname = table.Column<string>(name: "last_name", type: "character varying(128)", maxLength: 128, nullable: true),
                    prefernickname = table.Column<bool>(name: "prefer_nickname", type: "boolean", nullable: false),
                    picture = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    language = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    countrycode = table.Column<string>(name: "country_code", type: "character varying(2)", maxLength: 2, nullable: false),
                    timezone = table.Column<string>(name: "time_zone", type: "character varying(128)", maxLength: 128, nullable: false),
                    dateformat = table.Column<string>(name: "date_format", type: "character varying(64)", maxLength: 64, nullable: false),
                    timeformat = table.Column<string>(name: "time_format", type: "character varying(32)", maxLength: 32, nullable: false),
                    startofweek = table.Column<int>(name: "start_of_week", type: "integer", nullable: false, defaultValue: 1),
                    isactivated = table.Column<bool>(name: "is_activated", type: "boolean", nullable: false, defaultValue: false),
                    ban = table.Column<Ban>(type: "json", nullable: true),
                    roles = table.Column<string[]>(type: "text[]", nullable: false),
                    regip = table.Column<byte[]>(name: "reg_ip", type: "bytea", nullable: true),
                    regcountrycode = table.Column<string>(name: "reg_country_code", type: "character varying(2)", maxLength: 2, nullable: true),
                    updatedat = table.Column<DateTime>(name: "updated_at", type: "timestamp with time zone", rowVersion: true, nullable: false, defaultValueSql: "now()"),
                    createdat = table.Column<DateTime>(name: "created_at", type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "confirmations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    accountid = table.Column<long>(name: "account_id", type: "bigint", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    createdat = table.Column<DateTime>(name: "created_at", type: "timestamp with time zone", nullable: false),
                    expiresat = table.Column<DateTime>(name: "expires_at", type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_confirmations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    type = table.Column<byte>(type: "smallint", nullable: false),
                    identity = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    firstname = table.Column<string>(name: "first_name", type: "character varying(256)", maxLength: 256, nullable: true),
                    lastname = table.Column<string>(name: "last_name", type: "character varying(256)", maxLength: 256, nullable: true),
                    accountid = table.Column<long>(name: "account_id", type: "bigint", nullable: false),
                    createdat = table.Column<DateTime>(name: "created_at", type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_connections", x => x.id);
                    table.UniqueConstraint("AK_connections_type_identity", x => new { x.type, x.identity });
                    table.ForeignKey(
                        name: "FK_connections_accounts_account_id",
                        column: x => x.accountid,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    accountid = table.Column<long>(name: "account_id", type: "bigint", nullable: false),
                    email = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    isprimary = table.Column<bool>(name: "is_primary", type: "boolean", nullable: false),
                    confirmedat = table.Column<DateTime>(name: "confirmed_at", type: "timestamp with time zone", nullable: true),
                    createdat = table.Column<DateTime>(name: "created_at", type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_emails", x => x.id);
                    table.UniqueConstraint("AK_emails_email", x => x.email);
                    table.ForeignKey(
                        name: "FK_emails_accounts_account_id",
                        column: x => x.accountid,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_confirmations_account_id_type",
                table: "confirmations",
                columns: new[] { "account_id", "type" });

            migrationBuilder.CreateIndex(
                name: "IX_connections_account_id",
                table: "connections",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_emails_account_id",
                table: "emails",
                column: "account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "confirmations");

            migrationBuilder.DropTable(
                name: "connections");

            migrationBuilder.DropTable(
                name: "emails");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
