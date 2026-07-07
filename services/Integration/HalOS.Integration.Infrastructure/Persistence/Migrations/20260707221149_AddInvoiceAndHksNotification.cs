using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Integration.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceAndHksNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "hks_notification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    market_fee_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hks_notification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    scenario = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    commission_vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    invoice_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_hks_notification_tenant_id",
                table: "hks_notification",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_hks_notification_tenant_id_sale_transaction_id",
                table: "hks_notification",
                columns: new[] { "tenant_id", "sale_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id",
                table: "invoice",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_tenant_id_sale_transaction_id",
                table: "invoice",
                columns: new[] { "tenant_id", "sale_transaction_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hks_notification");

            migrationBuilder.DropTable(
                name: "invoice");
        }
    }
}
