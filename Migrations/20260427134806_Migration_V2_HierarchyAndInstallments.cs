using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class Migration_V2_HierarchyAndInstallments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Level",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "Categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Categories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "Icon", "Level", "Name", "ParentId", "Type" },
                values: new object[,]
                {
                    { 1, null, null, 1, "Gençler Apt.", null, 1 },
                    { 2, null, null, 1, "Tekirdağ", null, 1 },
                    { 3, null, null, 1, "Özberk Apt.", null, 1 },
                    { 4, null, null, 1, "Market", null, 1 },
                    { 5, null, null, 1, "Kasap", null, 1 },
                    { 6, null, null, 1, "Sigorta", null, 1 },
                    { 7, null, null, 1, "Vergi", null, 1 },
                    { 8, null, null, 1, "Eczane", null, 1 },
                    { 9, null, null, 1, "Kediler", null, 1 },
                    { 10, null, null, 1, "Telefonlar", null, 1 },
                    { 11, null, null, 1, "Araba", null, 1 },
                    { 100, null, null, 1, "Maaş", null, 0 },
                    { 12, null, null, 2, "Gençler", 6, 1 },
                    { 13, null, null, 2, "Yazlık", 6, 1 },
                    { 14, null, null, 2, "Özberk", 6, 1 },
                    { 15, null, null, 2, "Sercan", 10, 1 },
                    { 16, null, null, 2, "Hakkı", 10, 1 },
                    { 17, null, null, 2, "Ayşem", 10, 1 },
                    { 18, null, null, 2, "FR 2104", 11, 1 },
                    { 19, null, null, 2, "KC 105", 11, 1 },
                    { 20, null, null, 2, "Aidat", 1, 1 },
                    { 21, null, null, 2, "Doğalgaz", 1, 1 },
                    { 22, null, null, 2, "Elektrik", 1, 1 },
                    { 23, null, null, 2, "Su", 1, 1 },
                    { 24, null, null, 2, "İnternet", 1, 1 },
                    { 25, null, null, 2, "Aidat", 2, 1 },
                    { 26, null, null, 2, "Doğalgaz", 2, 1 },
                    { 27, null, null, 3, "DASK", 12, 1 },
                    { 28, null, null, 3, "Yangın", 12, 1 },
                    { 29, null, null, 3, "Kasko", 18, 1 },
                    { 30, null, null, 3, "Trafik Sigortası", 18, 1 },
                    { 31, null, null, 3, "MTV", 18, 1 },
                    { 32, null, null, 3, "Benzin", 18, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_ParentId",
                table: "Categories",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_Categories_ParentId",
                table: "Categories",
                column: "ParentId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_Categories_ParentId",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_ParentId",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Categories");
        }
    }
}
