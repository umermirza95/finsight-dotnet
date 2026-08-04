using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class NEW_CHANGE : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FSImportedTransactions_FSImportedTransactionId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSImportedTransactionId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FSImportedTransactionId",
                table: "Transactions");

            migrationBuilder.AddColumn<Guid>(
                name: "FSTransactionId",
                table: "FSImportedTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FSImportedTransactions_FSTransactionId",
                table: "FSImportedTransactions",
                column: "FSTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions",
                column: "FSTransactionId",
                principalTable: "Transactions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSImportedTransactions_Transactions_FSTransactionId",
                table: "FSImportedTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FSImportedTransactions_FSTransactionId",
                table: "FSImportedTransactions");

            migrationBuilder.DropColumn(
                name: "FSTransactionId",
                table: "FSImportedTransactions");

            migrationBuilder.AddColumn<string>(
                name: "FSImportedTransactionId",
                table: "Transactions",
                type: "character varying(200)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSImportedTransactionId",
                table: "Transactions",
                column: "FSImportedTransactionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FSImportedTransactions_FSImportedTransactionId",
                table: "Transactions",
                column: "FSImportedTransactionId",
                principalTable: "FSImportedTransactions",
                principalColumn: "Id");
        }
    }
}
