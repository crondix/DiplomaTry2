using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DiplomaTry2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261014010000_PrinterModelCharacteristics")]
public sealed class PrinterModelCharacteristics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("DeviceType", "PrinterModels", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>("DeviceFunctions", "PrinterModels", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("PrintTechnology", "PrinterModels", type: "nvarchar(80)", maxLength: 80, nullable: true);
        migrationBuilder.AddColumn<string>("MaximumPaperFormat", "PrinterModels", type: "nvarchar(40)", maxLength: 40, nullable: true);
        migrationBuilder.AddColumn<string>("MonochromeResolution", "PrinterModels", type: "nvarchar(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<int>("RecommendedMonthlyVolume", "PrinterModels", type: "int", nullable: true);
        migrationBuilder.AddColumn<int>("MaximumMonthlyVolume", "PrinterModels", type: "int", nullable: true);
        migrationBuilder.AddColumn<string>("Interfaces", "PrinterModels", type: "nvarchar(200)", maxLength: 200, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[] { "DeviceType", "DeviceFunctions", "PrintTechnology", "MaximumPaperFormat", "MonochromeResolution", "RecommendedMonthlyVolume", "MaximumMonthlyVolume", "Interfaces" })
            migrationBuilder.DropColumn(column, "PrinterModels");
    }
}
