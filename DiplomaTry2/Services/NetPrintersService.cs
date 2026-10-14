using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.InterFaces;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Services;

public sealed record PrinterImportResult(int ModelsAdded, int PrintersAdded, int PrintersUpdated, int WithoutModel);

public sealed class PrinterSourceException(string message, Exception inner) : Exception(message, inner);

public sealed class NetPrintersService(
    IPrinterSource injectedSource,
    PrintSourceSelection selection,
    IDbContextFactory<ApplicationDbContext> injectedFactory,
    ApplicationSettingsService? settings = null, ILogger<NetPrintersService>? logger = null)
{
    public async Task<PrinterImportResult> ImportAsync()
    {
        try {
            var result = await ImportCoreAsync();
            logger?.LogInformation("Импорт принтеров завершён. Моделей добавлено: {Models}; принтеров добавлено: {Added}; обновлено: {Updated}; без модели: {WithoutModel}.",
                result.ModelsAdded, result.PrintersAdded, result.PrintersUpdated, result.WithoutModel);
            return result;
        }
        catch (Exception ex) {
            logger?.LogError(ex, "Не удалось импортировать принтеры.");
            throw;
        }
    }

    private async Task<PrinterImportResult> ImportCoreAsync()
    {
        if (settings is not null) await settings.RequireAdminAsync();
        var globals = (injectedFactory as RuntimeDatabase)?.Snapshot();
        var snapshot = selection.Capture(globals?.PrintServer);
        var source = injectedSource is SelectedPrinterSource selected ? await selected.CaptureAsync(snapshot) : injectedSource;
        var contextFactory = injectedFactory is RuntimeDatabase runtime ? runtime.Capture(globals) : injectedFactory;
        var sourceKey = snapshot.IsFixture ? "fixture" : $"windows:{snapshot.ServerName.ToUpperInvariant()}";
        logger?.LogInformation("Начат импорт принтеров из {Source}.", sourceKey);
        if (sourceKey.Length > 200)
            throw new InvalidOperationException("Имя источника печати слишком длинное.");

        IReadOnlyList<NetworkPrinter> sourcePrinters;
        try
        {
            sourcePrinters = await source.GetPrintersAsync();
        }
        catch (Exception ex)
        {
            throw new PrinterSourceException($"Не удалось прочитать источник печати: {ex.Message}", ex);
        }

        var printers = new Dictionary<string, NetworkPrinter>(StringComparer.Ordinal);
        foreach (var printer in sourcePrinters)
        {
            if (string.IsNullOrWhiteSpace(printer.Name))
                throw new InvalidOperationException("Источник вернул принтер без имени очереди.");
            var key = QueueKey(printer.Name);
            if (key.Length > 450)
                throw new InvalidOperationException($"Имя очереди слишком длинное: {printer.Name}");
            printers[key] = printer;
        }

        await using var db = await contextFactory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var savedPrinters = await db.NetworkPrinters.Where(p => p.SourceKey == sourceKey).ToListAsync();
        var printerByKey = savedPrinters.ToDictionary(p => p.QueueKey, StringComparer.Ordinal);
        var added = 0;
        var updated = 0;
        var withoutModel = 0;
        foreach (var (key, incoming) in printers)
        {
            if (printerByKey.TryGetValue(key, out var saved))
            {
                // Queue properties are source managed. Comment remains operator editable.
                saved.ShareName = incoming.ShareName;
                saved.Name = incoming.Name.Trim();
                saved.Ip = incoming.Ip;
                saved.NonIPAddress = incoming.NonIPAddress;
                saved.DriverName = incoming.DriverName;
                saved.IsOnline = incoming.IsOnline;
                if (saved.PrinterModelId is null) withoutModel++;
                updated++;
            }
            else
            {
                db.NetworkPrinters.Add(new NetworkPrinter
                {
                    SourceKey = sourceKey,
                    QueueKey = key,
                    Name = incoming.Name.Trim(),
                    ShareName = incoming.ShareName,
                    Comment = incoming.Comment,
                    Ip = incoming.Ip,
                    NonIPAddress = incoming.NonIPAddress,
                    DriverName = incoming.DriverName,
                    IsOnline = incoming.IsOnline
                });
                added++;
                withoutModel++;
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new PrinterImportResult(0, added, updated, withoutModel);
    }

    private static string QueueKey(string name) => name.Trim().ToUpperInvariant();
}
