using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Text2Diagram_Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTempDiagrams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TempDiagrams",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "TempDiagrams",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TempDiagrams");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TempDiagrams");
        }
    }
}
