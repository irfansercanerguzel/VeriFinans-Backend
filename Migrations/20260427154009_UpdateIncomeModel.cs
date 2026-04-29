using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIncomeModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncomeType",
                table: "Incomes");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Incomes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_CategoryId",
                table: "Incomes",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_Categories_CategoryId",
                table: "Incomes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_Categories_CategoryId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_CategoryId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Incomes");

            migrationBuilder.AddColumn<string>(
                name: "IncomeType",
                table: "Incomes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
