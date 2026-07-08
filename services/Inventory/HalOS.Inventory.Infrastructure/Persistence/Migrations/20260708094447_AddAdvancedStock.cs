using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_stock_item_tenant_id_product_id",
                table: "stock_item");

            migrationBuilder.AddColumn<decimal>(
                name: "reorder_threshold",
                table: "stock_item",
                type: "numeric(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "warehouse_id",
                table: "stock_item",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "warehouse",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_tenant_id_warehouse_id_product_id",
                table: "stock_item",
                columns: new[] { "tenant_id", "warehouse_id", "product_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_tenant_id",
                table: "warehouse",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_tenant_id_code",
                table: "warehouse",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            // Tenant başına tek varsayılan depo (docs/06 S2.1): kısmi tekil indeks — yalnız
            // is_default=true satırlarda (tenant_id, is_default) benzersiz olur.
            migrationBuilder.CreateIndex(
                name: "IX_warehouse_tenant_id_is_default",
                table: "warehouse",
                columns: new[] { "tenant_id", "is_default" },
                unique: true,
                filter: "is_default");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "warehouse");

            migrationBuilder.DropIndex(
                name: "IX_stock_item_tenant_id_warehouse_id_product_id",
                table: "stock_item");

            migrationBuilder.DropColumn(
                name: "reorder_threshold",
                table: "stock_item");

            migrationBuilder.DropColumn(
                name: "warehouse_id",
                table: "stock_item");

            migrationBuilder.CreateIndex(
                name: "IX_stock_item_tenant_id_product_id",
                table: "stock_item",
                columns: new[] { "tenant_id", "product_id" },
                unique: true);
        }
    }
}
