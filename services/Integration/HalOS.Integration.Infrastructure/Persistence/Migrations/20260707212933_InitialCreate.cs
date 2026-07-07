using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Integration.Infrastructure.Persistence.Migrations
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
                name: "producer_receipt",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sale_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    buyer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issue_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    agri_withholding_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    farmer_ssk_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    net_payable = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producer_receipt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "producer_tax_profile",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    keeps_records = table.Column<bool>(type: "boolean", nullable: false),
                    agri_withholding_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    farmer_ssk_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producer_tax_profile", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receipt_deduction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    producer_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_deduction", x => x.id);
                    table.ForeignKey(
                        name: "FK_receipt_deduction_producer_receipt_producer_receipt_id",
                        column: x => x.producer_receipt_id,
                        principalTable: "producer_receipt",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_on_utc",
                table: "outbox_message",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_producer_receipt_tenant_id",
                table: "producer_receipt",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_producer_receipt_tenant_id_sale_transaction_id",
                table: "producer_receipt",
                columns: new[] { "tenant_id", "sale_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_producer_tax_profile_tenant_id",
                table: "producer_tax_profile",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_producer_tax_profile_tenant_id_producer_party_id",
                table: "producer_tax_profile",
                columns: new[] { "tenant_id", "producer_party_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_deduction_producer_receipt_id",
                table: "receipt_deduction",
                column: "producer_receipt_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_deduction_tenant_id",
                table: "receipt_deduction",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "producer_tax_profile");

            migrationBuilder.DropTable(
                name: "receipt_deduction");

            migrationBuilder.DropTable(
                name: "producer_receipt");
        }
    }
}
