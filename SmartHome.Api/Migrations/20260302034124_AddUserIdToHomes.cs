using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartHome.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToHomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Homes",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE h
                SET h.UserId = (
                    SELECT TOP (1) u.UserId
                    FROM Users u
                    ORDER BY u.UserId
                )
                FROM Homes h
                WHERE h.UserId IS NULL;
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM Homes WHERE UserId IS NULL)
                BEGIN
                    THROW 50001, 'Cannot migrate Homes.UserId: no Users available to backfill existing Homes rows.', 1;
                END
                """);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "Homes",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Homes_UserId",
                table: "Homes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Homes_Users_UserId",
                table: "Homes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Homes_Users_UserId",
                table: "Homes");

            migrationBuilder.DropIndex(
                name: "IX_Homes_UserId",
                table: "Homes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Homes");

        }
    }
}
