using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class trade_config_alpaca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlpacaApiKey",
                table: "TradingConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlpacaApiSecret",
                table: "TradingConfigs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ServerPort",
                table: "TradingConfigs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TradingConfigs_ServerIp",
                table: "TradingConfigs",
                column: "ServerIp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TradingConfigs_ServerIp",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "AlpacaApiKey",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "AlpacaApiSecret",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "ServerPort",
                table: "TradingConfigs");
        }
    }
}
