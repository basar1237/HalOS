using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalOS.Sales.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleTerm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "term",
                table: "sale_transaction",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "term",
                table: "sale_transaction");
        }
    }
}
