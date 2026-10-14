using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Migrations
{
    // The 2024 migration already creates the application tables. This migration
    // only records the later change to two nullable printer fields.
    public partial class InitialCreate2025 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NetworkPrinters",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ShareName",
                table: "NetworkPrinters",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [NetworkPrinters] SET [Name] = N'' WHERE [Name] IS NULL;");
            migrationBuilder.Sql("UPDATE [NetworkPrinters] SET [ShareName] = N'' WHERE [ShareName] IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "NetworkPrinters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShareName",
                table: "NetworkPrinters",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
