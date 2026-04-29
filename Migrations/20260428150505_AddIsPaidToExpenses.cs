using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class AddIsPaidToExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaid",
                table: "Expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaid",
                table: "Expenses");
        }
    }
}
