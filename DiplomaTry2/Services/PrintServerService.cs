
using System.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Net;
using System.Printing;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using DiplomaTry2.InterFaces;
using DiplomaModels;
using DevExpress.DocumentServices.ServiceModel.DataContracts;
namespace DiplomaTry2.Services
{

    public class PrintServerService 
    {

        public List<NetworkPrinter> GetListPrintersInfoFromPrintServer(string printServerName)
        {
            List<NetworkPrinter> printers = new List<NetworkPrinter>();

            try
            {
                PrintServer printServer = new PrintServer(printServerName);
                PrintQueueCollection printCollection = printServer.GetPrintQueues();

                foreach (var printQueue in printCollection)
                {
                    var printer = GetPrinterInfo(printQueue);
                    if (printer != null)
                    {
                        printers.Add(printer);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка: {e.Message} ");
                Console.WriteLine($"{e.StackTrace}");
            }

            return printers;
        }
        public List<PrinterModel>? GetPrintersModelsList(string printServerName)
        {
            List<PrinterModel> printersModels = new List<PrinterModel>();
            try
            {
                PrintServer printServer = new PrintServer(printServerName);
                PrintQueueCollection printCollection = printServer.GetPrintQueues();

                foreach (var printQueue in printCollection)
                {
                    IPAddress printIp;
                    if (printQueue.QueuePort.Name is not null && IPAddress.TryParse(printQueue.QueuePort.Name, out printIp))
                    {
                        if (!printQueue.IsOffline)
                        {
                            if (printersModels.FirstOrDefault(o => o.ModelName == printQueue.QueueDriver.Name) is null)
                            {
                                printersModels.Add(new PrinterModel
                                {
                                    ModelName = printQueue.QueueDriver.Name
                                });
                            }
                        }
                    }
                }

                return printersModels;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetPrinterInfoAsync Ошибка: {e.Message} ");
                Console.WriteLine($"{e.StackTrace}");
                return null;
            }
        }
        public NetworkPrinter? GetPrinterInfo(PrintQueue printQueue)
        {
            try
            {
                IPAddress printIp;
                if (printQueue.QueuePort.Name is not null && IPAddress.TryParse(printQueue.QueuePort.Name, out printIp))
                {
                    if (!printQueue.IsOffline)
                    {

                        //var printInfoFromSNMP = InfoFromSNMP.GetPrintersInfo(printQueue.QueuePort.Name);
                        //var SNMPName = printInfoFromSNMP?.FirstOrDefault(k => k.Key == "Name").Value;
                        string name = printQueue.QueueDriver.Name;
                        var test = printQueue.QueuePort;

                        //var modelSource = SNMPName != null ? "SNMPName" : "printQueue.QueueDriver.Name";
                        return new NetworkPrinter
                        {
                            ShareName = printQueue.Name,
                            Comment = printQueue.Comment,
                            Ip = printQueue.QueuePort.Name,
                            PrinterModel = new PrinterModel
                            {
                                ModelName = name,
                            },


                        };
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetPrinterInfoAsync Ошибка: {e.Message} ");
                Console.WriteLine($"{e.StackTrace}");
                return null;
            }
        }
    }
}
