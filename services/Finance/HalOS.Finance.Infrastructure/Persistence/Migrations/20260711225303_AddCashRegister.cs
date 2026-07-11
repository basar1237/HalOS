using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Finance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cash_register",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_register", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cash_movement",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_register_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    occurred_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_movement", x => x.id);
                    table.ForeignKey(
                        name: "FK_cash_movement_cash_register_cash_register_id",
                        column: x => x.cash_register_id,
                        principalTable: "cash_register",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cash_movement_cash_register_id",
                table: "cash_movement",
                column: "cash_register_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movement_tenant_id",
                table: "cash_movement",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_cash_movement_tenant_id_cash_register_id",
                table: "cash_movement",
                columns: new[] { "tenant_id", "cash_register_id" });

            migrationBuilder.CreateIndex(
                name: "IX_cash_register_tenant_id",
                table: "cash_register",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_movement");

            migrationBuilder.DropTable(
                name: "cash_register");
        }
    }
}
