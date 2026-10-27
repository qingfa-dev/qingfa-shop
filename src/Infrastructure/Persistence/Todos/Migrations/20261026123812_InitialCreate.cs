using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Todos.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "todo_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "todo_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todo_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "todos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    due_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    list_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_ids = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    created_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    modified_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    modified_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    deleted_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_todos", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_todo_categories_created_at_utc",
                table: "todo_categories",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_todo_categories_tenant_id",
                table: "todo_categories",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_todo_categories_tenant_id_is_deleted",
                table: "todo_categories",
                columns: new[] { "tenant_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_created_at_utc",
                table: "todo_lists",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_tenant_id",
                table: "todo_lists",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_todo_lists_tenant_id_is_deleted",
                table: "todo_lists",
                columns: new[] { "tenant_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "IX_todos_created_at_utc",
                table: "todos",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_todos_due_at_utc",
                table: "todos",
                column: "due_at_utc");

            migrationBuilder.CreateIndex(
                name: "IX_todos_list_id",
                table: "todos",
                column: "list_id");

            migrationBuilder.CreateIndex(
                name: "IX_todos_priority",
                table: "todos",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_todos_status",
                table: "todos",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_todos_tenant_id",
                table: "todos",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_todos_tenant_id_is_deleted",
                table: "todos",
                columns: new[] { "tenant_id", "is_deleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "todo_categories");

            migrationBuilder.DropTable(
                name: "todo_lists");

            migrationBuilder.DropTable(
                name: "todos");
        }
    }
}
