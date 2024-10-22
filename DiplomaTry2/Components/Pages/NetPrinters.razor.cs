

using DiplomaModels;

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
        NetPrintersService nps { get; set; }
        [Inject]
        IConfiguration AppConfig { get; set; }
        [Inject]
        private IDbContextFactory<ApplicationDbContext> _contextFactory { get; set; }

        async Task NetworkPrinterAddToDB()
        {
            await using (var context = _contextFactory.CreateDbContext())
            {
                List<NetworkPrinter>? printers = printserver.GetListNetPrintersInfoFromPrintServer($@"{AppConfig["printserver:name"]}");
               await nps.AddListNetworkPrinterToDBAsync(printers, context);
           }
        }

    }
}
