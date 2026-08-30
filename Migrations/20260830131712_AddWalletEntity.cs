using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddWalletEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FSWallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FSUserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FSCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    CreationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InitialBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSWallets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSWallets_AspNetUsers_FSUserId",
                        column: x => x.FSUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSWallets_FSCurrencies_FSCurrencyCode",
                        column: x => x.FSCurrencyCode,
                        principalTable: "FSCurrencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ""FSCurrencies"" (""Code"") 
                SELECT 'USD' 
                WHERE NOT EXISTS (SELECT 1 FROM ""FSCurrencies"" WHERE ""Code"" = 'USD');

                INSERT INTO ""FSWallets"" (""Id"", ""FSUserId"", ""Name"", ""FSCurrencyCode"", ""CreationDate"", ""InitialBalance"")
                SELECT gen_random_uuid(), ""Id"", 'Main Wallet', 'USD', NOW(), 0 FROM ""AspNetUsers"";
            ");

            migrationBuilder.AddColumn<Guid>(
                name: "FSWalletId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FSWalletId",
                table: "FSImportedTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"
                UPDATE ""Transactions"" SET ""FSWalletId"" = (SELECT ""Id"" FROM ""FSWallets"" WHERE ""FSUserId"" = ""Transactions"".""FSUserId"" LIMIT 1);
                UPDATE ""FSImportedTransactions"" SET ""FSWalletId"" = (SELECT ""Id"" FROM ""FSWallets"" WHERE ""FSUserId"" = ""FSImportedTransactions"".""FSUserId"" LIMIT 1);
            ");



            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FSWalletId",
                table: "Transactions",
                column: "FSWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_FSImportedTransactions_FSWalletId",
                table: "FSImportedTransactions",
                column: "FSWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_FSWallets_FSCurrencyCode",
                table: "FSWallets",
                column: "FSCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_FSWallets_FSUserId",
                table: "FSWallets",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FSImportedTransactions_FSWallets_FSWalletId",
                table: "FSImportedTransactions",
                column: "FSWalletId",
                principalTable: "FSWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_FSWallets_FSWalletId",
                table: "Transactions",
                column: "FSWalletId",
                principalTable: "FSWallets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSImportedTransactions_FSWallets_FSWalletId",
                table: "FSImportedTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_FSWallets_FSWalletId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "FSWallets");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_FSWalletId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_FSImportedTransactions_FSWalletId",
                table: "FSImportedTransactions");

            migrationBuilder.DropColumn(
                name: "FSWalletId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "FSWalletId",
                table: "FSImportedTransactions");
        }
    }
}
