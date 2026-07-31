using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class Update_ClosedTrades_Profits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        }
    }
}
