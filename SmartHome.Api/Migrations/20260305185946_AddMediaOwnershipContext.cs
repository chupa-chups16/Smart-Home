using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaOwnershipContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "MediaFiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeviceId",
                table: "MediaFiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "MediaFiles");
        }
    }
}
