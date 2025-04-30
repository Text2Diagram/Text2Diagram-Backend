using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Text2Diagram_Backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class m4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<object>(
                name: "Data",
                table: "Projects",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Projects",
                type: "text",
                nullable: false,
                oldClrType: typeof(object),
                oldType: "jsonb");
        }
    }
}
