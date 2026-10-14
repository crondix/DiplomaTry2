using System.Data;
using System.Security.Cryptography;
using System.Text;
using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.InterFaces;
using DiplomaTry2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DiplomaTry2.Services;

public sealed record PrintImportResult(int Read, int Added, int Skipped, string? Error = null);

public sealed class EventLogProcessingService(
    IPrintEventSource injectedSource, IDbContextFactory<ApplicationDbContext> injectedFactory,
    ApplicationSettingsService? settings = null, ILogger<EventLogProcessingService>? logger = null)
{
    private const int BatchSize = 500;
    public event Action<int, int, int>? AddProgressChanged;
    public event Action<string>? UniquePrinterFounded;

    // Each page, its related lookup rows, and the cursor commit together. A SQL application lock
    // serializes imports from the same journal across browser sessions and app processes.
    public async Task<PrintImportResult> DbActualizationAsync()
    {
        if (settings is not null) await settings.RequireAdminAsync();
        var globals = (injectedFactory as RuntimeDatabase)?.Snapshot();
        var eventSource = injectedSource is SelectedPrintEventSource selected ? await selected.CaptureAsync(globals?.PrintServer) : injectedSource;
        var contextFactory = injectedFactory is RuntimeDatabase runtime ? runtime.Capture(globals) : injectedFactory;
        var source = eventSource.SourceKey;
        var log = eventSource.LogName;
        var read = 0;
        var added = 0;
        var skipped = 0;
        logger?.LogInformation("Начат импорт событий печати из {Source}/{Log}.", source, log);
        try
        {
        while (true)
        {
            await using var db = await contextFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            await LockSourceAsync(db, source, log);
            var cursor = await db.PrintEventImportCursors
                .SingleOrDefaultAsync(c => c.SourceKey == source && c.LogName == log);
            var batch = await eventSource.ReadBatchAsync(cursor?.RecordId, cursor?.EventTime, BatchSize);
            if (eventSource.SourceKey != source || eventSource.LogName != log)
                throw new InvalidOperationException("Источник печати изменился во время импорта. Повторите операцию.");

            if (batch.CursorLost && cursor is not null)
            {
                db.PrintEventImportCursors.Remove(cursor);
                cursor = null;
            }
            var items = batch.Events.OrderBy(e => e.RecordId).ToArray();
            if (items.Length == 0)
            {
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                break;
            }
            if (items.Any(e => e.RecordId is null || e.DateTime is null ||
                               !string.Equals(e.SourceName, source, StringComparison.OrdinalIgnoreCase) ||
                               e.LogName != log))
                throw new FormatException("Источник вернул событие без полного идентификатора журнала.");

            var ids = items.Select(e => e.RecordId!.Value).Distinct().ToArray();
            var existing = await db.EventsSuccessfulPrinting.AsNoTracking()
                .Where(e => e.SourceKey == source && e.LogName == log && e.RecordId.HasValue && ids.Contains(e.RecordId.Value))
                .Select(e => new { e.RecordId, e.DateTime }).ToListAsync();
            var seen = existing.Select(e => (e.RecordId!.Value, e.DateTime!.Value)).ToHashSet();
            var newItems = items.Where(e => seen.Add((e.RecordId!.Value, e.DateTime!.Value))).ToArray();
            await AddEventsAsync(db, newItems, source, log);

            // The source returns records in ascending record ID order, independent of event time.
            var last = items[^1];
            if (cursor is null)
            {
                cursor = new PrintEventImportCursor { SourceKey = source, LogName = log };
                db.PrintEventImportCursors.Add(cursor);
            }
            cursor.RecordId = last.RecordId!.Value;
            cursor.EventTime = last.DateTime;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            read += items.Length;
            added += newItems.Length;
            skipped += items.Length - newItems.Length;
            AddProgressChanged?.Invoke(0, read, read);
            if (items.Length < BatchSize) break;
        }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Ошибка импорта событий из {Source}/{Log}. Прочитано: {Read}; добавлено: {Added}; пропущено: {Skipped}.", source, log, read, added, skipped);
            return new PrintImportResult(read, added, skipped, ex.Message);
        }
        logger?.LogInformation("Импорт событий из {Source}/{Log} завершён. Прочитано: {Read}; добавлено: {Added}; пропущено: {Skipped}.", source, log, read, added, skipped);
        return new PrintImportResult(read, added, skipped);
    }

    private static async Task LockSourceAsync(ApplicationDbContext db, string source, string log)
    {
        var connection = db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "DECLARE @result int; EXEC @result = sp_getapplock @Resource, 'Exclusive', 'Transaction', 15000; SELECT @result;";
        var resource = command.CreateParameter();
        resource.ParameterName = "@Resource";
        resource.Value = "PrintImport:" + Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes($"{source}\n{log}")));
        command.Parameters.Add(resource);
        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (result < 0)
            throw new TimeoutException("Не удалось получить блокировку импорта журнала печати.");
    }

    private async Task AddEventsAsync(ApplicationDbContext db, IReadOnlyList<EventPrint307> items, string source, string log)
    {
        if (items.Count == 0) return;
        static string Key(string name) => name.ToUpperInvariant().Replace(" ", "");
        // The existing TargetPrinters unique index is global. Use a source-scoped
        // internal lookup key without changing the SQL schema or historical rows.
        string PrinterKey(string name) => "SRC:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToLowerInvariant() + "\n" + Key(name))));
        var printerNames = items.Select(e => PrinterKey(e.PrinterName)).Distinct().ToArray();
        var senderNames = items.Select(e => Key(e.UserName)).Distinct().ToArray();
        var deviceNames = items.Select(e => Key(e.PCName)).Distinct().ToArray();
        var fileNames = items.Select(e => e.DocName).Distinct().ToArray();
        var printers = (await db.TargetPrinters.Where(e => printerNames.Contains(e.NameNormalized)).ToListAsync())
            .ToDictionary(e => e.NameNormalized);
        var networkSource = source == "fixture" ? source : "windows:" + source.ToUpperInvariant();
        var network = await db.NetworkPrinters.Where(e => (e.SourceKey == source || e.SourceKey == networkSource) && e.ShareName != null).ToListAsync();
        var networkByName = network.GroupBy(e => Key(e.ShareName!)).ToDictionary(g => g.Key, g => g.First());
        var senders = (await db.Senders.Where(e => senderNames.Contains(e.NameNormalized)).ToListAsync())
            .ToDictionary(e => e.NameNormalized);
        var devices = (await db.SenderDevices.Where(e => deviceNames.Contains(e.NameNormalized)).ToListAsync())
            .ToDictionary(e => e.NameNormalized);
        var names = (await db.DocumentNames.Where(e => fileNames.Contains(e.name)).ToListAsync())
            .ToDictionary(e => e.name);
        var files = (await db.SentPrintingFiles.Include(e => e.Name)
                .Where(e => fileNames.Contains(e.Name.name)).ToListAsync())
            .ToDictionary(e => (e.Name.name, e.Size, e.Pages));

        foreach (var item in items)
        {
            var printerKey = PrinterKey(item.PrinterName);
            if (!printers.TryGetValue(printerKey, out var printer))
            {
                networkByName.TryGetValue(Key(item.PrinterName), out var linked);
                if (linked is null) UniquePrinterFounded?.Invoke(item.PrinterName);
                printer = new TargetPrinter { Name = item.PrinterName, NameNormalized = printerKey, NetworkPrinter = linked };
                printers.Add(printerKey, printer);
                db.TargetPrinters.Add(printer);
            }
            var senderKey = Key(item.UserName);
            if (!senders.TryGetValue(senderKey, out var sender))
            {
                sender = new Sender { Name = item.UserName };
                senders.Add(senderKey, sender);
                db.Senders.Add(sender);
            }
            var deviceKey = Key(item.PCName);
            if (!devices.TryGetValue(deviceKey, out var device))
            {
                device = new SenderDevice { Name = item.PCName };
                devices.Add(deviceKey, device);
                db.SenderDevices.Add(device);
            }
            if (!names.TryGetValue(item.DocName, out var name))
            {
                name = new DocumentName { name = item.DocName };
                names.Add(item.DocName, name);
                db.DocumentNames.Add(name);
            }
            var fileKey = (item.DocName, item.Size, item.Page);
            if (!files.TryGetValue(fileKey, out var file))
            {
                file = new SentPrintingFile { Name = name, Size = item.Size, Pages = item.Page };
                files.Add(fileKey, file);
                db.SentPrintingFiles.Add(file);
            }
            db.EventsSuccessfulPrinting.Add(new EventSuccessfulPrinting
            {
                SourceKey = source, LogName = log, RecordId = item.RecordId,
                DateTime = item.DateTime, Sender = sender, SenderDevice = device,
                TargetPrinter = printer, SentPrintingFile = file
            });
        }
    }
}
