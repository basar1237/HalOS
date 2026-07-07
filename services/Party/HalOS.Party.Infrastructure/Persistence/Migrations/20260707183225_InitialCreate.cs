using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Party.Infrastructure.Persistence.Migrations
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
                name: "party",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    tckn = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    vkn = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    tax_office = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    keeps_records = table.Column<bool>(type: "boolean", nullable: false),
                    agri_withholding_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    farmer_ssk_rate = table.Column<decimal>(type: "numeric(7,4)", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_on_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_role",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_party_role_party_party_id",
                        column: x => x.party_id,
                        principalTable: "party",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_on_utc",
                table: "outbox_message",
                column: "processed_on_utc");

            migrationBuilder.CreateIndex(
                name: "IX_party_tenant_id",
                table: "party",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_party_tenant_id_tckn",
                table: "party",
                columns: new[] { "tenant_id", "tckn" },
                unique: true,
                filter: "tckn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_party_tenant_id_vkn",
                table: "party",
                columns: new[] { "tenant_id", "vkn" },
                unique: true,
                filter: "vkn IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_party_role_party_id_type",
                table: "party_role",
                columns: new[] { "party_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_role_tenant_id",
                table: "party_role",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message");

            migrationBuilder.DropTable(
                name: "party_role");

            migrationBuilder.DropTable(
                name: "party");
        }
    }
}
