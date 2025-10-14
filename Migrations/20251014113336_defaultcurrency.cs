using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class defaultcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultCurrency",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DefaultCurrency",
                table: "AspNetUsers",
                column: "DefaultCurrency");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_FSCurrency_DefaultCurrency",
                table: "AspNetUsers",
                column: "DefaultCurrency",
                principalTable: "FSCurrency",
                principalColumn: "Code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_FSCurrency_DefaultCurrency",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DefaultCurrency",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DefaultCurrency",
                table: "AspNetUsers");
        }
    }
}
