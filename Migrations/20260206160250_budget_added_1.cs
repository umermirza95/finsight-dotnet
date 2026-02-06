using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class budget_added_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FSBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FSUserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FSCurrencyCode = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSBudgets_AspNetUsers_FSUserId",
                        column: x => x.FSUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSBudgets_FSCurrencies_FSCurrencyCode",
                        column: x => x.FSCurrencyCode,
                        principalTable: "FSCurrencies",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FSBudgetCategories",
                columns: table => new
                {
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSBudgetCategories", x => new { x.BudgetId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_FSBudgetCategories_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSBudgetCategories_FSBudgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "FSBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FSBudgetPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSBudgetPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSBudgetPeriods_FSBudgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "FSBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FSBudgetCategories_CategoryId",
                table: "FSBudgetCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FSBudgetPeriods_BudgetId",
                table: "FSBudgetPeriods",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_FSBudgets_FSCurrencyCode",
                table: "FSBudgets",
                column: "FSCurrencyCode");

            migrationBuilder.CreateIndex(
                name: "IX_FSBudgets_FSUserId",
                table: "FSBudgets",
                column: "FSUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FSBudgetCategories");

            migrationBuilder.DropTable(
                name: "FSBudgetPeriods");

            migrationBuilder.DropTable(
                name: "FSBudgets");
        }
    }
}
