using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elwark.People.Infrastructure.Migrations.IntegrationEventLogContext
{
    public partial class InitialEventLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) =>
            migrationBuilder.CreateTable(
                "integration_event_logs",
                table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    event_type_name = table.Column<string>(nullable: false),
                    state = table.Column<int>(nullable: false),
                    times_sent = table.Column<int>(nullable: false),
                    creation_time = table.Column<DateTime>(nullable: false),
                    content = table.Column<string>("json", nullable: false)
                },
                constraints: table => { table.PrimaryKey("PK_integration_event_logs", x => x.id); });

        protected override void Down(MigrationBuilder migrationBuilder) =>
            migrationBuilder.DropTable(
                "integration_event_logs");
    }
}