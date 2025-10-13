using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class transaction1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FSUserId",
                table: "Transactions",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSCategoryId",
                table: "Transactions",
                column: "FSCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSSubCategoryId",
                table: "Transactions",
                column: "FSSubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSUserId",
                table: "Transactions",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_AspNetUsers_FSUserId",
                table: "Transactions",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Categories_FSCategoryId",
                table: "Transactions",
                column: "FSCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_SubCategories_FSSubCategoryId",
                table: "Transactions",
                column: "FSSubCategoryId",
                principalTable: "SubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_AspNetUsers_FSUserId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Categories_FSCategoryId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_SubCategories_FSSubCategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSCategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSSubCategoryId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSUserId",
                table: "Transactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "FSUserId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
