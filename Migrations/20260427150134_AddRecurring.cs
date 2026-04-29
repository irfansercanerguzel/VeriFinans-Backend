using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecurring",
                table: "Incomes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecurring",
                table: "Incomes");
        }
    }
}
