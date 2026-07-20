using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class MakeTradingConfigGlobal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TradingConfigs_AspNetUsers_FSUserId",
                table: "TradingConfigs");

            migrationBuilder.DropIndex(
                name: "IX_TradingConfigs_FSUserId",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "FSUserId",
                table: "TradingConfigs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FSUserId",
                table: "TradingConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TradingConfigs_FSUserId",
                table: "TradingConfigs",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TradingConfigs_AspNetUsers_FSUserId",
                table: "TradingConfigs",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
