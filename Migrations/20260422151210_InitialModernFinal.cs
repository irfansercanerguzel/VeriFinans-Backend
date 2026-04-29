using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VeriFinans.Migrations
{
    /// <inheritdoc />
    public partial class InitialModernFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Installment",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "CardId",
                table: "Expenses",
                newName: "CurrentInstallment");

            migrationBuilder.AlterColumn<string>(
                name: "IncomeType",
                table: "Incomes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Incomes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Incomes",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Expenses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "InstallmentCount",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Expenses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Expenses",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CreditCardId",
                table: "Expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CardName",
                table: "CreditCards",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ClosingDay",
                table: "CreditCards",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DueDay",
                table: "CreditCards",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_UserId",
                table: "Incomes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId",
                table: "Expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CreditCardId",
                table: "Expenses",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId",
                table: "Expenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_UserId",
                table: "CreditCards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditCards_User_UserId",
                table: "CreditCards",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_CreditCards_CreditCardId",
                table: "Expenses",
                column: "CreditCardId",
                principalTable: "CreditCards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_User_UserId",
                table: "Expenses",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_User_UserId",
                table: "Incomes",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditCards_User_UserId",
                table: "CreditCards");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Categories_CategoryId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_CreditCards_CreditCardId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_User_UserId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_User_UserId",
                table: "Incomes");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_UserId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CategoryId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CreditCardId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_UserId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CreditCards_UserId",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "CreditCardId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "ClosingDay",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "DueDay",
                table: "CreditCards");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "CurrentInstallment",
                table: "Expenses",
                newName: "CardId");

            migrationBuilder.AlterColumn<string>(
                name: "IncomeType",
                table: "Incomes",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Incomes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Incomes",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Expenses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InstallmentCount",
                table: "Expenses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Expenses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Expenses",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Expenses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Installment",
                table: "Expenses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PaidAmount",
                table: "Expenses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "RemainingAmount",
                table: "Expenses",
                type: "double precision",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CardName",
                table: "CreditCards",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
