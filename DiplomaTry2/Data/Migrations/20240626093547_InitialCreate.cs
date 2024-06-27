using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "id",
                table: "PrinterModel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "id",
                table: "PrinterModel",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
