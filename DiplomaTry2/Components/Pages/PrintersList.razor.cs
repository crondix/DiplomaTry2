using DiplomaModels;
using DiplomaTry2.Data;
using DiplomaTry2.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Components.Pages
{
    public partial class PrintersList
    {
        [Inject]
        PrintServerService printserver { get; set; }
        [Inject]
        NetPrintersService nps { get; set; }
        [Inject]
        IConfiguration AppConfig { get; set; }
        [Inject]
        private IDbContextFactory<ApplicationDbContext> _contextFactory { get; set; }

        NetworkPrinter printer { get; set; }
        List<NetworkPrinter>? printers { get; set; }
        string message { get; set; }
        bool Loading = false;

        class Error
        {
            public string Details { get; set; } = "";
        }
        async Task LoadingData()
        {
            Loading = true;
            printers = service.GetListNetPrintersInfoFromPrintServer(@$"{AppConfig["printserver:name"].ToString()}");

        }
    }
}
