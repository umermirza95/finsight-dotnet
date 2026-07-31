using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddNetProfitToClosedTrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NetProfit",
                table: "FSClosedTrades",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(@"
                UPDATE ""FSClosedTrades""
                SET ""NetProfit"" = ((s.""TradePrice"" - b.""TradePrice"") * b.""Quantity"") - (b.""Commission"" + s.""Commission"")
                FROM ""FSTrades"" b, ""FSTrades"" s
                WHERE ""FSClosedTrades"".""OrderOpenId"" = b.""ExternalId""
                  AND ""FSClosedTrades"".""OrderCloseId"" = s.""ExternalId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetProfit",
                table: "FSClosedTrades");
        }
    }
}
