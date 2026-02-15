using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rolla.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDriverStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DriverStatus",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverStatus",
                table: "AspNetUsers");
        }
    }
}
