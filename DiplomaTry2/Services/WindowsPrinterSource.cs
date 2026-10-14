using System.Net;
using System.Printing;
using DiplomaModels;
using DiplomaTry2.InterFaces;

namespace DiplomaTry2.Services;

public sealed class WindowsPrinterSource(PrintSourceSelection selection) : IPrinterSource
{
    public Task<IReadOnlyList<PrinterModel>> GetModelsAsync()
    {
        var serverPath = @"\\" + selection.ServerName;
        return Task.Run<IReadOnlyList<PrinterModel>>(() =>
        {
            using var server = new PrintServer(serverPath);
            var models = new Dictionary<string, PrinterModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var queue in server.GetPrintQueues())
            {
                using (queue)
                {
                    var name = queue.QueueDriver.Name;
                    if (string.IsNullOrWhiteSpace(name) || models.ContainsKey(name)) continue;
                    models.Add(name, new PrinterModel { ModelName = name });
                }
            }
            return models.Values.ToList();
        });
    }

    public Task<IReadOnlyList<NetworkPrinter>> GetPrintersAsync()
    {
        var serverPath = @"\\" + selection.ServerName;
        return Task.Run<IReadOnlyList<NetworkPrinter>>(() =>
        {
            using var server = new PrintServer(serverPath);
            var printers = new List<NetworkPrinter>();
            foreach (var queue in server.GetPrintQueues())
            {
                using (queue)
                {
                    var port = queue.QueuePort.Name;
                    var driver = queue.QueueDriver.Name;
                    printers.Add(new NetworkPrinter
                    {
                        Name = queue.Name,
                        ShareName = queue.ShareName,
                        Comment = queue.Comment,
                        Ip = IPAddress.TryParse(port, out _) ? port : null,
                        NonIPAddress = IPAddress.TryParse(port, out _) ? null : port,
                        DriverName = string.IsNullOrWhiteSpace(driver) ? null : driver,
                        IsOnline = !queue.IsOffline
                    });
                }
            }
            return printers;
        });
    }
}
