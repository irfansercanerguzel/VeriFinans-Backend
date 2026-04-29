using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "Icon", "Level", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 33, null, null, 2, "Elektrik", 2, 1 },
                    { 34, null, null, 2, "Su", 2, 1 },
                    { 35, null, null, 2, "İnternet", 2, 1 },
                    { 36, null, null, 2, "Aidat", 3, 1 },
                    { 37, null, null, 2, "Doğalgaz", 3, 1 },
                    { 38, null, null, 2, "Elektrik", 3, 1 },
                    { 39, null, null, 2, "Su", 3, 1 },
                    { 40, null, null, 2, "İnternet", 3, 1 },
                    { 41, null, null, 3, "Kasko", 19, 1 },
                    { 42, null, null, 3, "Trafik Sigortası", 19, 1 },
                    { 43, null, null, 3, "MTV", 19, 1 },
                    { 44, null, null, 3, "Benzin", 19, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 44);
        }
    }
}
