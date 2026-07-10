using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.ColdChain.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: true),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    before_json = table.Column<string>(type: "jsonb", nullable: true),
                    after_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cold_storage_unit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    min_temp_c = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    max_temp_c = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cold_storage_unit", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_message",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    occurred_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sensor_reading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cold_storage_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                    temperature_c = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    humidity_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor_reading", x => x.id);
                    table.ForeignKey(
                        name: "FK_sensor_reading_cold_storage_unit_cold_storage_unit_id",
                        column: x => x.cold_storage_unit_id,
                        principalTable: "cold_storage_unit",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_tenant_id_created_on_utc",
                table: "audit_log",
                columns: new[] { "tenant_id", "created_on_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_cold_storage_unit_tenant_id",
                table: "cold_storage_unit",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_on_utc",
                table: "outbox_message",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_reading_cold_storage_unit_id",
                table: "sensor_reading",
                column: "cold_storage_unit_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_reading_tenant_id",
                table: "sensor_reading",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sensor_reading_tenant_id_cold_storage_unit_id_occurred_at",
                table: "sensor_reading",
                columns: new[] { "tenant_id", "cold_storage_unit_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "sensor_reading");

            migrationBuilder.DropTable(
                name: "cold_storage_unit");
        }
    }
}
