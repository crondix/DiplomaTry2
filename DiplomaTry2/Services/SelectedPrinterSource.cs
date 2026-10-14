using DiplomaModels;
using DiplomaTry2.InterFaces;

namespace DiplomaTry2.Services;

public sealed class SelectedPrinterSource(
    PrintSourceSelection selection,
    FixturePrintSource fixture,
    WindowsPrinterSource windows,
    ApplicationSettingsService? settings = null) : IPrinterSource
{
    public IPrinterSource Capture(PrintSourceSelection snapshot) =>
        snapshot.IsFixture ? new FixturePrintSource(snapshot) : new WindowsPrinterSource(snapshot);
    public async Task<IPrinterSource> CaptureAsync(PrintSourceSelection snapshot)
    {
        if (snapshot.IsFixture && settings is not null) await settings.RequireAdminAsync();
        return Capture(snapshot);
    }
    public async Task<IReadOnlyList<PrinterModel>> GetModelsAsync() =>
        await (await CaptureAsync(selection.Capture())).GetModelsAsync();

    public async Task<IReadOnlyList<NetworkPrinter>> GetPrintersAsync() =>
        await (await CaptureAsync(selection.Capture())).GetPrintersAsync();
}
