using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rolla.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchRadiusToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSearchRadius",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSearchTime",
                table: "Trips",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentSearchRadius",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "LastSearchTime",
                table: "Trips");
        }
    }
}
