using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FSTransactionSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FSUserId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FSCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FSCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    FSSubCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SubType = table.Column<string>(type: "text", nullable: true),
                    TransactionExternalId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSTransactionSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSTransactionSuggestions_AspNetUsers_FSUserId",
                        column: x => x.FSUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSTransactionSuggestions_Categories_FSCategoryId",
                        column: x => x.FSCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSTransactionSuggestions_FSCurrencies_FSCurrencyCode",
                        column: x => x.FSCurrencyCode,
                        principalTable: "FSCurrencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSTransactionSuggestions_SubCategories_FSSubCategoryId",
                        column: x => x.FSSubCategoryId,
                        principalTable: "SubCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FSTransactionSuggestions_FSCategoryId",
                table: "FSTransactionSuggestions",
                column: "FSCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FSTransactionSuggestions_FSCurrencyCode",
                table: "FSTransactionSuggestions",
                column: "FSCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_FSTransactionSuggestions_FSSubCategoryId",
                table: "FSTransactionSuggestions",
                column: "FSSubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FSTransactionSuggestions_FSUserId",
                table: "FSTransactionSuggestions",
                column: "FSUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FSTransactionSuggestions");
        }
    }
}
