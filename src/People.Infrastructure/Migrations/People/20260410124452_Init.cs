#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using People.Domain.ValueObjects;

namespace People.Infrastructure.Migrations.People;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "accounts",
            table => new
            {
                id = table.Column<long>("bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                nickname = table.Column<string>("character varying(64)", maxLength: 64, nullable: false),
                first_name = table.Column<string>("character varying(128)", maxLength: 128, nullable: true),
                last_name = table.Column<string>("character varying(128)", maxLength: 128, nullable: true),
                use_nickname = table.Column<bool>("boolean", nullable: false),
                picture = table.Column<string>("character varying(2048)", maxLength: 2048, nullable: false),
                region_code = table.Column<string>("character varying(2)", maxLength: 2, nullable: false,
                    defaultValue: "--"),
                country_code = table.Column<string>("character varying(2)", maxLength: 2, nullable: false,
                    defaultValue: "--"),
                time_zone = table.Column<string>("character varying(128)", maxLength: 128, nullable: false),
                locale = table.Column<string>("character varying(12)", maxLength: 12, nullable: false),
                roles = table.Column<string[]>("text[]", nullable: false),
                ban = table.Column<Ban>("jsonb", nullable: true),
                is_activated = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                reg_ip = table.Column<byte[]>("bytea", nullable: false),
                reg_country_code = table.Column<string>("character varying(2)", maxLength: 2, nullable: false),
                last_log_in =
                    table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at =
                    table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                created_at =
                    table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                xmin = table.Column<uint>("xid", rowVersion: true, nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_accounts", x => x.id); });

        migrationBuilder.CreateTable(
            "confirmations",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                account_id = table.Column<long>("bigint", nullable: false),
                type = table.Column<int>("integer", nullable: false),
                code = table.Column<string>("character varying(16)", maxLength: 16, nullable: false),
                expires_at = table.Column<DateTime>("timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_confirmations", x => x.id); });

        migrationBuilder.CreateTable(
            "outbox_consumers",
            table => new
            {
                message_id = table.Column<Guid>("uuid", nullable: false),
                consumer = table.Column<string>("character varying(256)", maxLength: 256, nullable: false),
                processed_at = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_outbox_consumers", x => new { x.message_id, x.consumer }); });

        migrationBuilder.CreateTable(
            "outbox_messages",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                type = table.Column<string>("character varying(256)", maxLength: 256, nullable: false),
                payload = table.Column<string>("jsonb", nullable: false),
                error = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                status = table.Column<int>("integer", nullable: false),
                attempts = table.Column<int>("integer", nullable: false),
                next_retry_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                processed_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                occurred_at = table.Column<DateTime>("timestamp with time zone", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_outbox_messages", x => x.id); });

        migrationBuilder.CreateTable(
            "connections",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                type = table.Column<byte>("smallint", nullable: false),
                identity = table.Column<string>("character varying(64)", maxLength: 64, nullable: false),
                first_name = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                last_name = table.Column<string>("character varying(256)", maxLength: 256, nullable: true),
                account_id = table.Column<long>("bigint", nullable: false),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false,
                    defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_connections", x => x.id);
                table.UniqueConstraint("AK_connections_type_identity", x => new { x.type, x.identity });
                table.ForeignKey(
                    "FK_connections_accounts_account_id",
                    x => x.account_id,
                    "accounts",
                    "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            "emails",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                account_id = table.Column<long>("bigint", nullable: false),
                email = table.Column<string>("character varying(512)", maxLength: 512, nullable: false),
                is_primary = table.Column<bool>("boolean", nullable: false),
                confirmed_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false,
                    defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_emails", x => x.id);
                table.UniqueConstraint("AK_emails_email", x => x.email);
                table.ForeignKey(
                    "FK_emails_accounts_account_id",
                    x => x.account_id,
                    "accounts",
                    "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            "IX_confirmations_account_id_type",
            "confirmations",
            new[] { "account_id", "type" },
            unique: true);

        migrationBuilder.CreateIndex(
            "IX_connections_account_id",
            "connections",
            "account_id");

        migrationBuilder.CreateIndex(
            "IX_emails_account_id",
            "emails",
            "account_id");

        migrationBuilder.CreateIndex(
            "IX_outbox_messages_processed_at_next_retry_at_occurred_at",
            "outbox_messages",
            new[] { "processed_at", "next_retry_at", "occurred_at" },
            filter: "processed_at IS NULL");

        migrationBuilder.CreateIndex(
            "IX_outbox_messages_processed_at_occurred_at",
            "outbox_messages",
            new[] { "processed_at", "occurred_at" },
            filter: "processed_at IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "confirmations");

        migrationBuilder.DropTable(
            "connections");

        migrationBuilder.DropTable(
            "emails");

        migrationBuilder.DropTable(
            "outbox_consumers");

        migrationBuilder.DropTable(
            "outbox_messages");

        migrationBuilder.DropTable(
            "accounts");
    }
}
