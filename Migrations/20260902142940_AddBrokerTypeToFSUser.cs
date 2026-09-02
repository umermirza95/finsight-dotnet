using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace finsight_dotnet.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerTypeToFSUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredBroker",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredBroker",
                table: "AspNetUsers");
        }
    }
}
