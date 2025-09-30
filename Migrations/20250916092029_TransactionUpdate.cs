using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class TransactionUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Comment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    SubType = table.Column<string>(type: "text", nullable: true),
                    Mode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");
        }
    }
}
