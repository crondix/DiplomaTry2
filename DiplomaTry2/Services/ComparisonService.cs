

using DiplomaModels;

using DiplomaTry2.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.Devices;

namespace DiplomaTry2.Services
{
    public class ComparisonService
    {
        [Inject]
        NetPrintersService nps { get; set; }


       async Task LinkTargetPrinterWithNetworkPrinterAsync(ApplicationDbContext context)
        {

            foreach(var printer in context.TargetPrinters)
            {
                if(context.NetworkPrinters.FirstOrDefault(np=> np.ShareName==printer.NameNormalized) is null)
                {
                    var NewPrinter = new NetworkPrinter { ShareName = printer.NameNormalized };
                    nps.AddPrinterToDBAsync(NewPrinter, context);
                }
            }
        }
    }
}
