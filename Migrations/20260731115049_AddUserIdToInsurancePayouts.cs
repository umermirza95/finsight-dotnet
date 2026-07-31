using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToInsurancePayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FSUserId",
                table: "FSInsurancePayouts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FSInsurancePayouts_FSUserId",
                table: "FSInsurancePayouts",
                column: "FSUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_FSInsurancePayouts_AspNetUsers_FSUserId",
                table: "FSInsurancePayouts",
                column: "FSUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FSInsurancePayouts_AspNetUsers_FSUserId",
                table: "FSInsurancePayouts");

            migrationBuilder.DropIndex(
                name: "IX_FSInsurancePayouts_FSUserId",
                table: "FSInsurancePayouts");

            migrationBuilder.DropColumn(
                name: "FSUserId",
                table: "FSInsurancePayouts");
        }
    }
}
