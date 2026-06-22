using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddFSUserIdToClosedTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FSUserId",
                table: "FSClosedTrades",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FSClosedTrades_FSUserId",
                table: "FSClosedTrades",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FSClosedTrades_AspNetUsers_FSUserId",
                table: "FSClosedTrades",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSClosedTrades_AspNetUsers_FSUserId",
                table: "FSClosedTrades");

            migrationBuilder.DropIndex(
                name: "IX_FSClosedTrades_FSUserId",
                table: "FSClosedTrades");

            migrationBuilder.DropColumn(
                name: "FSUserId",
                table: "FSClosedTrades");
        }
    }
}
