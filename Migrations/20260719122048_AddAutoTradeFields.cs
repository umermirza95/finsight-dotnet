using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoTradeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoTrade",
                table: "TradingConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "DistancePerTranche",
                table: "TradingConfigs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SharesPerTranche",
                table: "TradingConfigs",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoTrade",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "DistancePerTranche",
                table: "TradingConfigs");

            migrationBuilder.DropColumn(
                name: "SharesPerTranche",
                table: "TradingConfigs");
        }
    }
}
