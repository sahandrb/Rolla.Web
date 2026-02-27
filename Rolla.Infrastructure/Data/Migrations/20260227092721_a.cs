using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rolla.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class a : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverDocuments_AspNetUsers_ApplicationUserId",
                table: "DriverDocuments");

            migrationBuilder.DropIndex(
                name: "IX_DriverDocuments_ApplicationUserId",
                table: "DriverDocuments");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "DriverDocuments");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverDocuments_AspNetUsers_DriverId",
                table: "DriverDocuments",
                column: "DriverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DriverDocuments_AspNetUsers_DriverId",
                table: "DriverDocuments");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "DriverDocuments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverDocuments_ApplicationUserId",
                table: "DriverDocuments",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DriverDocuments_AspNetUsers_ApplicationUserId",
                table: "DriverDocuments",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
