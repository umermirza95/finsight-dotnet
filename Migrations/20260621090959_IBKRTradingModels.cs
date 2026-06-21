using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class IBKRTradingModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IBKRQueryId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IBKRToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FSTrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FSUserId = table.Column<string>(type: "text", nullable: false),
                    Ticker = table.Column<string>(type: "text", nullable: false),
                    TradePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TradeDirection = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Commission = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSTrades", x => x.Id);
                    table.UniqueConstraint("AK_FSTrades_ExternalId", x => x.ExternalId);
                    table.ForeignKey(
                        name: "FK_FSTrades_AspNetUsers_FSUserId",
                        column: x => x.FSUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FSClosedTrades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderOpenId = table.Column<string>(type: "text", nullable: false),
                    OrderCloseId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FSClosedTrades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FSClosedTrades_FSTrades_OrderCloseId",
                        column: x => x.OrderCloseId,
                        principalTable: "FSTrades",
                        principalColumn: "ExternalId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FSClosedTrades_FSTrades_OrderOpenId",
                        column: x => x.OrderOpenId,
                        principalTable: "FSTrades",
                        principalColumn: "ExternalId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FSClosedTrades_OrderCloseId",
                table: "FSClosedTrades",
                column: "OrderCloseId");

            migrationBuilder.CreateIndex(
                name: "IX_FSClosedTrades_OrderOpenId",
                table: "FSClosedTrades",
                column: "OrderOpenId");

            migrationBuilder.CreateIndex(
                name: "IX_FSTrades_FSUserId",
                table: "FSTrades",
                column: "FSUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FSClosedTrades");

            migrationBuilder.DropTable(
                name: "FSTrades");

            migrationBuilder.DropColumn(
                name: "IBKRQueryId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IBKRToken",
                table: "AspNetUsers");
        }
    }
}
