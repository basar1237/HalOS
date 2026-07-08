using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Integration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductPassport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_passport",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consignment_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    passport_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_passport", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_passport_tenant_id",
                table: "product_passport",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_passport_tenant_id_consignment_item_id",
                table: "product_passport",
                columns: new[] { "tenant_id", "consignment_item_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_passport");
        }
    }
}
