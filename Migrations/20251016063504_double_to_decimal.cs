using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class double_to_decimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRate",
                table: "FSExchangeRates",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "ExchangeRate",
                table: "FSExchangeRates",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }
    }
}
