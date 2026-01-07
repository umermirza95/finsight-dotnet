using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class supported_currencies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_FSCurrency_DefaultCurrency",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_FSExchangeRate_FromCurrency",
                table: "FSExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_FSExchangeRate_ToCurrency",
                table: "FSExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FSCurrency_FSCurrencyCode",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FSCurrency",
                table: "FSCurrency");

            migrationBuilder.RenameTable(
                name: "FSCurrency",
                newName: "FSCurrencies");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FSCurrencies",
                table: "FSCurrencies",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_FSCurrencies_DefaultCurrency",
                table: "AspNetUsers",
                column: "DefaultCurrency",
                principalTable: "FSCurrencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FSExchangeRates_FSCurrencies_From",
                table: "FSExchangeRates",
                column: "From",
                principalTable: "FSCurrencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FSExchangeRates_FSCurrencies_To",
                table: "FSExchangeRates",
                column: "To",
                principalTable: "FSCurrencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FSCurrencies_FSCurrencyCode",
                table: "Transactions",
                column: "FSCurrencyCode",
                principalTable: "FSCurrencies",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_FSCurrencies_DefaultCurrency",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_FSExchangeRates_FSCurrencies_From",
                table: "FSExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_FSExchangeRates_FSCurrencies_To",
                table: "FSExchangeRates");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FSCurrencies_FSCurrencyCode",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FSCurrencies",
                table: "FSCurrencies");

            migrationBuilder.RenameTable(
                name: "FSCurrencies",
                newName: "FSCurrency");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FSCurrency",
                table: "FSCurrency",
                column: "Code");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_FSCurrency_DefaultCurrency",
                table: "AspNetUsers",
                column: "DefaultCurrency",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FSExchangeRate_FromCurrency",
                table: "FSExchangeRates",
                column: "From",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FSExchangeRate_ToCurrency",
                table: "FSExchangeRates",
                column: "To",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FSCurrency_FSCurrencyCode",
                table: "Transactions",
                column: "FSCurrencyCode",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
