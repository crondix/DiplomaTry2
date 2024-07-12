using DiplomaModels;
using DiplomaTry2.InterFaces;
using System.Net.Http;
using System.Text.Json;

using DiplomaTry2.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore.Internal;
using DiplomaTry2.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using DiplomaTry2.Models;

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





        async Task Actualization()
        {
            try
            {
                Adding = true;
                await eventLogProcessing?.DbActualization();
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
                        .Include(e=>e.SenderDevice)
                         .Include(e=>e.TargetPrinter)
                          .ThenInclude(d => d.NetworkPrinter)
                            .ThenInclude(np => np.PrinterModel)
                              .ThenInclude(pm => pm.PaperSizes)
                        .Include(e=>e.Sender)
                        .Include(e=>e.SentPrintingFile)
                        .ToListAsync();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine($"Ошибка загрузки записей из базы данных : \n{e.Message} \n{e.InnerException}");
            }


        }


        //protected override async Task OnInitializedAsync()
        //{

        //}

    }
}
