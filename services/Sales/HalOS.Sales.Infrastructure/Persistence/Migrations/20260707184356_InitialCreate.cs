using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "consignment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    dispatch_note_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consignment", x => x.id);
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
                name: "producer_rate_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agri_withholding_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    farmer_ssk_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producer_rate_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sale_transaction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consignment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sold_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    is_within_market = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_cancelled = table.Column<bool>(type: "boolean", nullable: false),
                    cancellation_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_transaction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consignment_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    consignment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consignment_item", x => x.id);
                    table.ForeignKey(
                        name: "FK_consignment_item_consignment_consignment_id",
                        column: x => x.consignment_id,
                        principalTable: "consignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "commission_calculation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    vat_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_commission_calculation", x => x.id);
                    table.ForeignKey(
                        name: "FK_commission_calculation_sale_transaction_sale_transaction_id",
                        column: x => x.sale_transaction_id,
                        principalTable: "sale_transaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "deduction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_deduction", x => x.id);
                    table.ForeignKey(
                        name: "FK_deduction_sale_transaction_sale_transaction_id",
                        column: x => x.sale_transaction_id,
                        principalTable: "sale_transaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sale_line",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    unit = table.Column<string>(type: "text", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    line_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_line", x => x.id);
                    table.ForeignKey(
                        name: "FK_sale_line_sale_transaction_sale_transaction_id",
                        column: x => x.sale_transaction_id,
                        principalTable: "sale_transaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "settlement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    due_date = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settlement", x => x.id);
                    table.ForeignKey(
                        name: "FK_settlement_sale_transaction_sale_transaction_id",
                        column: x => x.sale_transaction_id,
                        principalTable: "sale_transaction",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_commission_calculation_sale_transaction_id",
                table: "commission_calculation",
                column: "sale_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_commission_calculation_tenant_id",
                table: "commission_calculation",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_tenant_id",
                table: "consignment",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_tenant_id_producer_party_id",
                table: "consignment",
                columns: new[] { "tenant_id", "producer_party_id" });

            migrationBuilder.CreateIndex(
                name: "IX_consignment_item_consignment_id",
                table: "consignment_item",
                column: "consignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_consignment_item_tenant_id",
                table: "consignment_item",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_deduction_sale_transaction_id",
                table: "deduction",
                column: "sale_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_deduction_tenant_id",
                table: "deduction",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_on_utc",
                table: "outbox_message",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_producer_rate_profile_tenant_id",
                table: "producer_rate_profile",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_producer_rate_profile_tenant_id_producer_party_id",
                table: "producer_rate_profile",
                columns: new[] { "tenant_id", "producer_party_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_line_sale_transaction_id",
                table: "sale_line",
                column: "sale_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_line_tenant_id",
                table: "sale_line",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_transaction_tenant_id",
                table: "sale_transaction",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_sale_transaction_tenant_id_buyer_party_id",
                table: "sale_transaction",
                columns: new[] { "tenant_id", "buyer_party_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_transaction_tenant_id_operation_id",
                table: "sale_transaction",
                columns: new[] { "tenant_id", "operation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_transaction_tenant_id_sold_at",
                table: "sale_transaction",
                columns: new[] { "tenant_id", "sold_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sale_transaction_tenant_id_status",
                table: "sale_transaction",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_settlement_sale_transaction_id",
                table: "settlement",
                column: "sale_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_settlement_tenant_id",
                table: "settlement",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_settlement_tenant_id_status_due_date",
                table: "settlement",
                columns: new[] { "tenant_id", "status", "due_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "commission_calculation");

            migrationBuilder.DropTable(
                name: "consignment_item");

            migrationBuilder.DropTable(
                name: "deduction");

            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "producer_rate_profile");

            migrationBuilder.DropTable(
                name: "sale_line");

            migrationBuilder.DropTable(
                name: "settlement");

            migrationBuilder.DropTable(
                name: "consignment");

            migrationBuilder.DropTable(
                name: "sale_transaction");
        }
    }
}
