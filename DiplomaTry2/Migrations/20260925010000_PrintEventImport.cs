using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260925010000_PrintEventImport")]
public sealed class PrintEventImport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SourceKey", "EventsSuccessfulPrinting", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<string>("LogName", "EventsSuccessfulPrinting", type: "nvarchar(200)", maxLength: 200, nullable: true);
        migrationBuilder.AddColumn<long>("RecordId", "EventsSuccessfulPrinting", type: "bigint", nullable: true);
        migrationBuilder.CreateTable(
            name: "PrintEventImportCursors",
            columns: table => new
            {
                SourceKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                LogName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                RecordId = table.Column<long>(type: "bigint", nullable: false),
                EventTime = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_PrintEventImportCursors", x => new { x.SourceKey, x.LogName }));
        migrationBuilder.CreateIndex(
            name: "IX_EventsSuccessfulPrinting_SourceKey_LogName_RecordId_DateTime",
            table: "EventsSuccessfulPrinting",
            columns: new[] { "SourceKey", "LogName", "RecordId", "DateTime" },
            unique: true,
            filter: "[RecordId] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("PrintEventImportCursors");
        migrationBuilder.DropIndex("IX_EventsSuccessfulPrinting_SourceKey_LogName_RecordId_DateTime", "EventsSuccessfulPrinting");
        migrationBuilder.DropColumn("SourceKey", "EventsSuccessfulPrinting");
        migrationBuilder.DropColumn("LogName", "EventsSuccessfulPrinting");
        migrationBuilder.DropColumn("RecordId", "EventsSuccessfulPrinting");
    }
}
