using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace DiplomaTry2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261014020000_PrinterModelImage")]
public sealed class PrinterModelImage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<byte[]>("ImageData", "PrinterModels", type: "varbinary(max)", nullable: true);
        migrationBuilder.AddColumn<string>("ImageContentType", "PrinterModels", type: "nvarchar(40)", maxLength: 40, nullable: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ImageData", "PrinterModels");
        migrationBuilder.DropColumn("ImageContentType", "PrinterModels");
    }
}
