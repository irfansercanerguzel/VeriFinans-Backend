using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCreditCardStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDay",
                table: "CreditCards");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueDay",
                table: "CreditCards",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
