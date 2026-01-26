using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class useridtofile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSFiles_Transactions_FSTransactionId",
                table: "FSFiles");

            migrationBuilder.AddColumn<string>(
                name: "FSUserId",
                table: "FSFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FSFiles_FSUserId",
                table: "FSFiles",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FSFiles_AspNetUsers_FSUserId",
                table: "FSFiles",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FSFiles_Transactions_FSTransactionId",
                table: "FSFiles",
                column: "FSTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSFiles_AspNetUsers_FSUserId",
                table: "FSFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_FSFiles_Transactions_FSTransactionId",
                table: "FSFiles");

            migrationBuilder.DropIndex(
                name: "IX_FSFiles_FSUserId",
                table: "FSFiles");

            migrationBuilder.DropColumn(
                name: "FSUserId",
                table: "FSFiles");

            migrationBuilder.AddForeignKey(
                name: "FK_FSFiles_Transactions_FSTransactionId",
                table: "FSFiles",
                column: "FSTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
