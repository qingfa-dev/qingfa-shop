using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_store_events",
                schema: "messaging",
                columns: table => new
                {
                    StreamId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    EventVersion = table.Column<int>(type: "integer", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_store_events", x => new { x.StreamId, x.Version });
                });

            migrationBuilder.CreateTable(
                name: "event_store_snapshots",
                schema: "messaging",
                columns: table => new
                {
                    StreamId = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "jsonb", nullable: false),
                    TakenOnUtc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_store_snapshots", x => x.StreamId);
                });

            migrationBuilder.CreateTable(
                name: "projection_checkpoints",
                schema: "messaging",
                columns: table => new
                {
                    Projection = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedOnUtc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projection_checkpoints", x => x.Projection);
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_store_events_EventId",
                schema: "messaging",
                table: "event_store_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_event_store_events_Position",
                schema: "messaging",
                table: "event_store_events",
                column: "Position",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_store_events",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "event_store_snapshots",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "projection_checkpoints",
                schema: "messaging");
        }
    }
}
