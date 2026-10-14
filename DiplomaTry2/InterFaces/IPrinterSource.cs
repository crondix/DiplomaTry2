using DiplomaModels;

namespace DiplomaTry2.InterFaces;

public interface IPrinterSource
{
    Task<IReadOnlyList<PrinterModel>> GetModelsAsync();
    Task<IReadOnlyList<NetworkPrinter>> GetPrintersAsync();
}
