using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiplomaTry2.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260925000000_PrinterSourceIdentity")]
public sealed class PrinterSourceIdentity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SourceKey", "NetworkPrinters", type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: "legacy");
        migrationBuilder.AddColumn<string>("QueueKey", "NetworkPrinters", type: "nvarchar(450)", maxLength: 450, nullable: false, defaultValue: "");

        // Existing queues have no proven source. Keep each one distinct without discarding data.
        migrationBuilder.Sql("UPDATE [NetworkPrinters] SET [QueueKey] = CONCAT(LEFT(UPPER(LTRIM(RTRIM(COALESCE([Name], N'')))), 430), N'#', [Id]);");
        // Adopt the three fixtures created during stage 3 only in the dedicated restoration database.
        migrationBuilder.Sql("IF DB_NAME() = N'Diploma_Restoration_Test' UPDATE [NetworkPrinters] SET [SourceKey] = N'fixture', [QueueKey] = UPPER([Name]) WHERE [Name] IN (N'TEST_HP_01', N'TEST_CANON_02', N'TEST_KYOCERA_03') AND (SELECT COUNT(*) FROM [NetworkPrinters] p2 WHERE p2.[Name] = [NetworkPrinters].[Name]) = 1;");
        migrationBuilder.CreateIndex("IX_NetworkPrinters_SourceKey_QueueKey", "NetworkPrinters", new[] { "SourceKey", "QueueKey" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_NetworkPrinters_SourceKey_QueueKey", "NetworkPrinters");
        migrationBuilder.DropColumn("QueueKey", "NetworkPrinters");
        migrationBuilder.DropColumn("SourceKey", "NetworkPrinters");
    }
}
