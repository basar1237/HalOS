using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "stock_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock_movement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "text", nullable: false),
                    signed_quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    ref_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movement", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_movement_stock_item_stock_item_id",
                        column: x => x.stock_item_id,
                        principalTable: "stock_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_on_utc",
                table: "outbox_message",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_tenant_id",
                table: "stock_item",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_tenant_id_product_id",
                table: "stock_item",
                columns: new[] { "tenant_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_stock_item_id",
                table: "stock_movement",
                column: "stock_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_tenant_id",
                table: "stock_movement",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_tenant_id_stock_item_id_kind_ref_id",
                table: "stock_movement",
                columns: new[] { "tenant_id", "stock_item_id", "kind", "ref_id" },
                unique: true,
                filter: "ref_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movement_tenant_id_stock_item_id_occurred_at",
                table: "stock_movement",
                columns: new[] { "tenant_id", "stock_item_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "stock_movement");

            migrationBuilder.DropTable(
                name: "stock_item");
        }
    }
}
