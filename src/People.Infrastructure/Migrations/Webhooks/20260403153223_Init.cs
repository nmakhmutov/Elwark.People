using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace People.Infrastructure.Migrations.Webhooks
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_consumers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<byte>(type: "smallint", nullable: false),
                    method = table.Column<byte>(type: "smallint", nullable: false),
                    destination_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_consumers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<byte>(type: "smallint", nullable: false),
                    status = table.Column<byte>(type: "smallint", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    retry_after = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_webhook_consumers_type",
                table: "webhook_consumers",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_messages_status_retry_after",
                table: "webhook_messages",
                columns: new[] { "status", "retry_after" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_consumers");

            migrationBuilder.DropTable(
                name: "webhook_messages");
        }
    }
}
