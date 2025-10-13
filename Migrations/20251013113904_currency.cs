using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class currency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Transactions",
                newName: "FSCurrencyCode");

            migrationBuilder.AlterColumn<string>(
                name: "FSUserId",
                table: "Categories",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "FSCurrency",
                columns: table => new
                {
                    Code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSCurrency", x => x.Code);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSCurrencyCode",
                table: "Transactions",
                column: "FSCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FSUserId",
                table: "Categories",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categories_AspNetUsers_FSUserId",
                table: "Categories",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FSCurrency_FSCurrencyCode",
                table: "Transactions",
                column: "FSCurrencyCode",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categories_AspNetUsers_FSUserId",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FSCurrency_FSCurrencyCode",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "FSCurrency");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSCurrencyCode",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_FSUserId",
                table: "Categories");

            migrationBuilder.RenameColumn(
                name: "FSCurrencyCode",
                table: "Transactions",
                newName: "Currency");

            migrationBuilder.AlterColumn<Guid>(
                name: "FSUserId",
                table: "Categories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
