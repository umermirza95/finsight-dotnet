using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureImportedTransactionCascadeNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions",
                column: "FSTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions");

            migrationBuilder.AddForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions",
                column: "FSTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }
    }
}
