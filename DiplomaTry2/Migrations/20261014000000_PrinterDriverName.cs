using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DiplomaTry2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261014000000_PrinterDriverName")]
public sealed class PrinterDriverName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("DriverName", "NetworkPrinters", type: "nvarchar(max)", nullable: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn("DriverName", "NetworkPrinters");
}
