using System.Windows;

using DiplomaModels;

using DiplomaTry2.Components.Pages;
using DiplomaTry2.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Services
{
    public class NetPrintersService
    {

        [Inject]
        PrintServerService printserver { get; set; }
        [Inject]
        IConfiguration AppConfig { get; set; }
        [Inject]
        private IDbContextFactory<ApplicationDbContext> _contextFactory { get; set; }

       public async Task AddPrinterToDBAsync(NetworkPrinter printer, ApplicationDbContext context)
        {

            if (context.NetworkPrinters.FirstOrDefault(n => n.Name == printer.Name) is null)
            {
                var model = context.PrinterModels.FirstOrDefault(m => m.NormalizedModelName == printer.PrinterModel.NormalizedModelName);
                if (model != null)
                {
                    printer.PrinterModel = model;
                }
                await context.NetworkPrinters.AddAsync(printer);
                await context.SaveChangesAsync();
            }
        }
        


     public async Task AddListNetworkPrinterToDBAsync(List<NetworkPrinter>? printers, ApplicationDbContext context)
        {

            
         
                Console.WriteLine($"Start Add Net Printers");
                foreach (var printer in printers)
                {

                   await AddPrinterToDBAsync(printer, context);
                }

                Console.WriteLine($"End Add Net Printers");
            

        }
    }
}
