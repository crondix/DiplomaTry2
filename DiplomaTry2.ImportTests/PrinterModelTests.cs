using System.ComponentModel.DataAnnotations;
using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace DiplomaTry2.ImportTests;

public sealed class PrinterModelTests
{
    [Fact]
    public void Sql_server_snapshot_and_driver_migration_match_current_model()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        Assert.False(db.Database.HasPendingModelChanges());
        var script = db.GetService<IMigrator>().GenerateScript("20260925010000_PrintEventImport", "20261014000000_PrinterDriverName");
        Assert.Contains("ADD [DriverName] nvarchar(max) NULL", script);
        Assert.DoesNotContain("DROP TABLE", script);
        var characteristics = db.GetService<IMigrator>().GenerateScript("20261014000000_PrinterDriverName", "20261014010000_PrinterModelCharacteristics");
        Assert.Contains("ADD [DeviceType] nvarchar(80) NULL", characteristics);
        Assert.Contains("ADD [MaximumMonthlyVolume] int NULL", characteristics);
    }

    [Fact]
    public async Task Non_admin_cannot_change_models_or_assignments()
    {
        using var database = new TestDatabase();
        using var user = new StoreFixture();
        var service = new PrinterModelService(database, user.Settings(admin: false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.SaveAsync(new() { Name = "HP" }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddSelectedAsync(["HP"]));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DeleteAsync(1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AssignAsync(1, 1));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task Selected_names_only_are_added_once_and_characteristics_remain_unknown()
    {
        using var database = new TestDatabase();
        var service = new PrinterModelService(database);
        Assert.Equal(2, await service.AddSelectedAsync([" HP M404dn ", "hp m404DN", "Kyocera P2040dn"]));
        Assert.Equal(0, await service.AddSelectedAsync(["HP M404dn"]));
        var models = await service.ListAsync();
        Assert.Equal(2, models.Count);
        Assert.All(models, m => { Assert.Null(m.IsColor); Assert.Null(m.IsDuplexing); Assert.Null(m.MaxCopyPerPrint); });
        Assert.DoesNotContain(models, m => m.ModelName.Contains("Universal"));
    }

    [Fact]
    public async Task Manual_create_edit_duplicate_and_validation()
    {
        using var database = new TestDatabase();
        var service = new PrinterModelService(database);
        await service.SaveAsync(new() { Name = "HP M404dn", IsColor = false, IsDuplexing = true, MaxCopyPerPrint = 99 });
        var model = Assert.Single(await service.ListAsync());
        await service.SaveAsync(new() { Id = model.Id, Name = "HP M405dn", IsColor = null, IsDuplexing = false });
        var updated = Assert.Single(await service.ListAsync());
        Assert.Equal(model.Id, updated.Id); Assert.Equal("HP M405dn", updated.ModelName);
        Assert.Null(updated.IsColor); Assert.False(updated.IsDuplexing); Assert.Null(updated.MaxCopyPerPrint);
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = " hp m405DN " }));
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = " " }));
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = "Invalid", MaxCopyPerPrint = 0 }));
    }

    [Fact]
    public async Task Card_characteristics_are_saved_edited_and_validated()
    {
        using var database = new TestDatabase();
        var service = new PrinterModelService(database);
        await service.SaveAsync(new()
        {
            Name = "Kyocera TASKalfa 3050ci", DeviceType = " МФУ лазерное ", DeviceFunctions = "копир, принтер, сканер, факс",
            PrintTechnology = "лазерная", IsColor = true, MaximumPaperFormat = "A3", MonochromeResolution = "600x600 dpi",
            RecommendedMonthlyVolume = 2000, MaximumMonthlyVolume = 20000, Interfaces = "Ethernet (RJ-45), USB", IsDuplexing = true
        });
        var listed = Assert.Single(await service.ListAsync());
        var card = await service.GetAsync(listed.Id);
        Assert.Equal("МФУ лазерное", card.DeviceType); Assert.Equal("A3", card.MaximumPaperFormat);
        Assert.Equal("600x600 dpi", card.MonochromeResolution); Assert.Equal(2000, card.RecommendedMonthlyVolume);
        Assert.Equal(20000, card.MaximumMonthlyVolume); Assert.Equal("Ethernet (RJ-45), USB", card.Interfaces);
        Assert.Equal("копир, принтер, сканер, факс", card.DeviceFunctions); Assert.Equal("лазерная", card.PrintTechnology);
        await service.SaveAsync(new() { Id = card.Id, Name = card.ModelName, Interfaces = " Wi-Fi ", DeviceType = " " });
        var edited = await service.GetAsync(card.Id);
        Assert.Equal("Wi-Fi", edited.Interfaces); Assert.Null(edited.DeviceType); Assert.Null(edited.MaximumMonthlyVolume);
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = "Invalid", RecommendedMonthlyVolume = 3000, MaximumMonthlyVolume = 2000 }));
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = "Invalid", MaximumMonthlyVolume = 0 }));
        await Assert.ThrowsAsync<ValidationException>(() => service.SaveAsync(new() { Name = "Invalid", Interfaces = new string('x', 201) }));
    }

    [Fact]
    public async Task Import_never_creates_models_and_preserves_assignment_when_driver_changes()
    {
        using var database = new TestDatabase();
        var source = new FakeSource();
        var selection = new PrintSourceSelection(new() { Mode = "Fixture" });
        var importer = new NetPrintersService(source, selection, database);
        Assert.Equal(new PrinterImportResult(0, 2, 0, 2), await importer.ImportAsync());
        var service = new PrinterModelService(database);
        Assert.Empty(await service.ListAsync());
        await service.SaveAsync(new() { Name = "HP M404dn" });
        var model = Assert.Single(await service.ListAsync());
        await using (var db = database.CreateDbContext())
        {
            var printer = await db.NetworkPrinters.SingleAsync(p => p.Name == "Queue 1");
            await service.AssignAsync(printer.Id, model.Id);
        }
        source.Driver = "KX DRIVER for Universal Printing";
        Assert.Equal(new PrinterImportResult(0, 0, 2, 1), await importer.ImportAsync());
        await using var check = database.CreateDbContext();
        Assert.Equal(model.Id, (await check.NetworkPrinters.SingleAsync(p => p.Name == "Queue 1")).PrinterModelId);
        Assert.All(await check.NetworkPrinters.ToListAsync(), p => Assert.Equal(source.Driver, p.DriverName));
        Assert.Single(await service.ListAsync());
    }

    [Fact]
    public async Task Delete_clears_assignments_and_preserves_printers_and_paper_sizes()
    {
        using var database = new TestDatabase();
        var service = new PrinterModelService(database);
        int id;
        await using (var db = database.CreateDbContext())
        {
            var model = new PrinterModel { ModelName = "HP M404dn", PaperSizes = [new() { Name = "A4" }] };
            db.NetworkPrinters.Add(new() { Name = "Queue", QueueKey = "QUEUE", PrinterModel = model });
            await db.SaveChangesAsync(); id = model.Id;
        }
        Assert.Equal(1, await service.CountPrintersAsync(id));
        await service.DeleteAsync(id);
        await using var check = database.CreateDbContext();
        Assert.Empty(await check.PrinterModels.ToListAsync());
        Assert.Null(Assert.Single(await check.NetworkPrinters.ToListAsync()).PrinterModelId);
        Assert.Single(await check.PaperSizes.ToListAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AssignAsync(1, id));
    }

    private sealed class FakeSource : IPrinterSource
    {
        public string Driver { get; set; } = "HP Universal Printing PCL 5";
        public Task<IReadOnlyList<PrinterModel>> GetModelsAsync() => throw new InvalidOperationException("Queue import must not fetch models.");
        public Task<IReadOnlyList<NetworkPrinter>> GetPrintersAsync() => Task.FromResult<IReadOnlyList<NetworkPrinter>>([
            new() { Name = "Queue 1", DriverName = Driver }, new() { Name = "Queue 2", DriverName = Driver }
        ]);
    }

    private sealed class TestDatabase : IDbContextFactory<ApplicationDbContext>, IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly DbContextOptions<ApplicationDbContext> options;
        public TestDatabase()
        {
            connection.Open();
            options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
            using var db = CreateDbContext(); db.Database.EnsureCreated();
        }
        public ApplicationDbContext CreateDbContext() => new(options);
        public void Dispose() => connection.Dispose();
    }
}
