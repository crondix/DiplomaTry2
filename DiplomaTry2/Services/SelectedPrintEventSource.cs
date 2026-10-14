using DiplomaTry2.InterFaces;
using DiplomaTry2.Models;

namespace DiplomaTry2.Services;

public sealed class SelectedPrintEventSource(
    PrintSourceSelection selection,
    FixturePrintSource fixture,
    WindowsPrintEventSource windows,
    ApplicationSettingsService? settings = null) : IPrintEventSource
{
    private IPrintEventSource Current => selection.IsFixture ? fixture : windows;
    public IPrintEventSource Capture(string? server = null)
    {
        var snapshot = selection.Capture(server);
        return snapshot.IsFixture ? new FixturePrintSource(snapshot) : new WindowsPrintEventSource(snapshot);
    }
    public string SourceKey => Current.SourceKey;
    public string LogName => Current.LogName;
    public async Task<IPrintEventSource> CaptureAsync(string? server = null)
    {
        var result = Capture(server);
        if (result is FixturePrintSource && settings is not null) await settings.RequireAdminAsync();
        return result;
    }
    public async Task<PrintEventBatch> ReadBatchAsync(long? afterRecordId, DateTime? anchorTime, int limit) =>
        await (await CaptureAsync()).ReadBatchAsync(afterRecordId, anchorTime, limit);
}
