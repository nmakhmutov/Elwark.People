#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace People.Infrastructure.Migrations.Webhooks;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "webhook_consumers",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                type = table.Column<byte>("smallint", nullable: false),
                method = table.Column<byte>("smallint", nullable: false),
                destination_url = table.Column<string>("character varying(2048)", maxLength: 2048, nullable: false),
                token = table.Column<string>("character varying(256)", maxLength: 256, nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_webhook_consumers", x => x.id); });

        migrationBuilder.CreateTable(
            "webhook_messages",
            table => new
            {
                id = table.Column<Guid>("uuid", nullable: false),
                account_id = table.Column<long>("bigint", nullable: false),
                type = table.Column<byte>("smallint", nullable: false),
                status = table.Column<byte>("smallint", nullable: false),
                attempts = table.Column<int>("integer", nullable: false, defaultValue: 0),
                occurred_at = table.Column<DateTime>("timestamp with time zone", nullable: false),
                retry_after = table.Column<DateTime>("timestamp with time zone", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_webhook_messages", x => x.id); });

        migrationBuilder.CreateIndex(
            "IX_webhook_consumers_type",
            "webhook_consumers",
            "type");

        migrationBuilder.CreateIndex(
            "IX_webhook_messages_status_retry_after",
            "webhook_messages",
            new[] { "status", "retry_after" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            "webhook_consumers");

        migrationBuilder.DropTable(
            "webhook_messages");
    }
}
