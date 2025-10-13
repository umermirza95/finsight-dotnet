using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class transaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseAmount",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "SubCategoryId",
                table: "Transactions",
                newName: "FSSubCategoryId");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "Transactions",
                newName: "FSUserId");

            migrationBuilder.AddColumn<Guid>(
                name: "FSCategoryId",
                table: "Transactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FSCategoryId",
                table: "Transactions");

            migrationBuilder.RenameColumn(
                name: "FSUserId",
                table: "Transactions",
                newName: "CategoryId");

            migrationBuilder.RenameColumn(
                name: "FSSubCategoryId",
                table: "Transactions",
                newName: "SubCategoryId");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseAmount",
                table: "Transactions",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
