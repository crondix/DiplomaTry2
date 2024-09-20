using DiplomaModels;

using DiplomaTry2.Data;
using DiplomaTry2.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace DiplomaTry2.Components.Pages
{
    public partial class EventsSuccessfullyPrintingView
    {

        [Inject]
        private EventLogService? eventLog { get; set; }
        [Inject]
        private EventLogProcessingService? eventLogProcessing { get; set; }
        [Inject]
        private IDbContextFactory<ApplicationDbContext> DbContextFactory { get; set; }



        List<EventSuccessfulPrinting>? EventsSuccessfulPrinting;
        string? message;
        bool Loading = false;
        bool Adding = false;
        bool IsCheked = false;
        bool? IsAdditionSuccessful;
        string AdditionErrorMessage = "";
        double _procent=0;
        double _currentItem = 0;
        double _totalyItem = 0;
        
        private void OnProgressChanged(int percent, int currentItem, int totalyItem)
        {
            InvokeAsync(() =>
            {
                _procent = percent;
                _currentItem = currentItem;
                _totalyItem= totalyItem;
                StateHasChanged();
            });
           
        }



            async Task Actualization()
        {
            try
            {
                
                eventLogProcessing.AddProgressChanged += OnProgressChanged;
                eventLogProcessing.Error += (string _message) => { IsAdditionSuccessful = false; AdditionErrorMessage = _message; Adding = false; return; };
                Adding = true;
                
               await eventLogProcessing?.DbActualizationAsync();
                await LoadData();
                Adding = false;
                Loading = !Loading;
                IsAdditionSuccessful = true; 
            }
            catch (Exception e)
            {
                IsAdditionSuccessful = false;
                AdditionErrorMessage = e?.InnerException?.Message ?? "";
            }
        }

        async Task LoadData()
        {
            Loading = true;

            try
            {
                await using (var context = DbContextFactory.CreateDbContext())
                {
                    EventsSuccessfulPrinting = await context.EventsSuccessfulPrinting
                        .Include(e => e.SenderDevice)
                         .Include(e => e.TargetPrinter)
                          .ThenInclude(d => d.NetworkPrinter)
                            .ThenInclude(np => np.PrinterModel)
                              .ThenInclude(pm => pm.PaperSizes)
                        .Include(e => e.Sender)
                        .Include(e => e.SentPrintingFile)
                         .ThenInclude(dn=>dn.Name)
                        .OrderBy(record => record.DateTime).Reverse()
                          .Take(50)
                        .ToListAsync();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка загрузки записей из базы данных : \n{e.Message} \n{e.InnerException}");
            }


        }


      

    }
}
