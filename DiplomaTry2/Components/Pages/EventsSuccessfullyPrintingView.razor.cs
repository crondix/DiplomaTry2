//using DiplomaModels;
//using DiplomaTry2.InterFaces;
//using System.Net.Http;
//using System.Text.Json;

//using DiplomaTry2.Services;
//using Microsoft.AspNetCore.Components;
//using Microsoft.EntityFrameworkCore.Internal;
//using DiplomaTry2.Data;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Identity.Client;
//using DiplomaTry2.Models;

//namespace DiplomaTry2.Components.Pages
//{
//    public class EventsSuccessfullyPrintingView
//    {

//        [Inject]
//        private PrintServerService? printServerService { get; set; }  
//        [Inject]
//        private IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; }  
//        [Inject]
//        private IConfiguration appConfig { get; set; } 
//        [Inject]
//        private NavigationManager NavigationManager { get; set; }


//       // List<EventPrint307>? PrinterModelsFromPrintServer;
//       // List<PrinterModel>? PrinterModelsFromBD;
        
//       // List<PrinterModel> SelectedPrinters = new List<PrinterModel>();


        




//        async Task AddDB()
//        {
//            try
//            {
//                Adding = true;
//                await using var DbContext = DbContextFactory.CreateDbContext();
//                DbContext.PrinterModels.AddRange(SelectedPrinters);

//                DbContext.SaveChanges();
//                Adding = false;
//                SelectedPrinters = new List<PrinterModel>();
//                PrinterModelsFromPrintServer = null;
//                Loading = !Loading;
//                IsAdditionSuccessful = true;
//            }
//            catch (Exception e)
//            {
//                IsAdditionSuccessful = false;
//                AdditionErrorMessage = e?.InnerException?.Message ?? "";
//            }
//        }
//        async Task LoadData()
//        {
//            Loading = true;

//            try
//            {
//                PrinterModelsFromPrintServer = await printServerService.GetPrintersModelsListAsync(@"\\vm-print");
//                await using var DbContext = DbContextFactory.CreateDbContext();
//                PrinterModelsFromBD = DbContext.PrinterModels.ToList();
//                foreach (var printer in PrinterModelsFromPrintServer.ToList())
//                {
//                    if (PrinterModelsFromBD?.FirstOrDefault(p => p?.NormalizedModelName == printer?.NormalizedModelName) is not null)
//                    {
//                        PrinterModelsFromPrintServer.Remove(printer); // Указываем конкретный элемент для удаления
//                    }
//                }

//            }
//            catch (Exception e)
//            {
//                Console.WriteLine($"Ошибка загрузки данных с принт сервера : \n{e.Message}");
//            }


//        }


//        //protected override async Task OnInitializedAsync()
//        //{

//        //}

//    }
//}
