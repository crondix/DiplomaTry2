using DevExpress.Pdf.Native;

using DiplomaTry2.Data;
using DiplomaTry2.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace DiplomaTry2.Components.Pages
{
    public partial class NetPrinters
    {
        [Inject]
        PrintServerService printserver { get; set; }
        [Inject]
        IConfiguration Configuration { get; set; }
        [Inject]
        private IDbContextFactory<ApplicationDbContext> _contextFactory { get; set; }

        async Task NetworkPrinterAddToDB()
        {

            var printers = printserver.GetListNetPrintersInfoFromPrintServer($@"{Configuration["printserver:name"].ToString()}");
            await using (var context = _contextFactory.CreateDbContext())
            {
               
                foreach (var printer in printers)
                {
                    var model = context.PrinterModels.FirstOrDefault(m => m.NormalizedModelName == printer.PrinterModel.NormalizedModelName);
                    if (model != null)
                    {
                        printer.PrinterModelId = model.Id;
                    }
                    context.NetworkPrinters.Add(printer);

                }
            }

        }

    }
}
