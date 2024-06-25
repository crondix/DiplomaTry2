using DiplomaModels;

using System.Printing;

namespace DiplomaTry2.InterFaces
{
    public interface IPrintServerService
    {
        List<NetworkPrinter> GetListPrintersInfoFromPrintServer(string printServerName);
        NetworkPrinter? GetPrinterInfo(PrintQueue printQueue);
        List<string> GetPrintersModelsList(string printServerName);
    }
}
