using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddInsurancePayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FSInsurancePayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FSClosedTradeId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoveredAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSInsurancePayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSInsurancePayouts_FSClosedTrades_FSClosedTradeId",
                        column: x => x.FSClosedTradeId,
                        principalTable: "FSClosedTrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FSInsurancePayouts_FSClosedTradeId",
                table: "FSInsurancePayouts",
                column: "FSClosedTradeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FSInsurancePayouts");
        }
    }
}
