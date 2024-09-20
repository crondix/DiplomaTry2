using DiplomaModels;

using DiplomaTry2.Data;
using DiplomaTry2.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace DiplomaTry2.Services
{
    public partial class EventLogProcessingService
    {
        private readonly EventLogService _eventLogService;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        public delegate void AddEventsError(string message);
        public event AddEventsError? Error;
        public event Action<int, int, int>? AddProgressChanged;
        public event Action<string>? UniquePrinterFounded;
      

        public EventLogProcessingService(EventLogService eventLogService, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _eventLogService = eventLogService;
            _contextFactory = contextFactory;
        }



        public async Task AddEventsToDBAsync(List<EventSuccessfulPrinting> events)
        {
            await using (var context = _contextFactory.CreateDbContext())
            {
                try
                {
                    var eventFromDb = await context.EventsSuccessfulPrinting.ToListAsync();

                    List<EventSuccessfulPrinting>? exist;
               
                    var lastEventDB = eventFromDb.LastOrDefault();

                    if (lastEventDB is null)
                    {
                        exist = events;
                        exist.Reverse();
                    }
                    else
                    {
                        exist = events.Where(w => w.DateTime > lastEventDB.DateTime).ToList();
                        exist.Reverse();
                    }

                    if (!exist.Any())
                        return;

                    Console.WriteLine($"Adding Events Start. Events count: {exist.Count}");
                    double proccent = 0.01;
                    Console.Write($"\rCompleted: 0%  (0 of {exist.Count})");

                    int totalEvents = exist.Count;
                    //double percentIncrement = 100.0 / totalEvents;
                    //double currentPercent = 0;

                    for (var i = 0; i < exist.Count; i++)
                    {

                        await AddEventToContextAsync(exist[i], context);

                        if (i == Math.Round((exist.Count * proccent)))
                        {
                            AddProgressChanged?.Invoke((int)Math.Round(proccent * 100), i + 1, totalEvents);
                            Console.Write($"\rCompleted: {Math.Round(proccent * 100)}%  ({i} of {exist.Count})");
                            proccent += 0.01;
                        }

                    }

                    await context.SaveChangesAsync();
                    Console.Write($"\rCompleted: 100%  ({exist.Count} of {exist.Count})");
                    Console.WriteLine($"\nAdding Events Completed.");


                }
                catch (Exception ex)
                {
                    Error?.Invoke("Ошибка при попытке актуализации данных : \n{ex.Message} \n{ex.InnerException}");
                    Console.WriteLine($"Ошибка при попытке актуализации данных : \n{ex.Message} \n{ex.InnerException}");
                }
            }
        }
        async Task AddEventToContextAsync(EventSuccessfulPrinting Event, ApplicationDbContext context)
        {
            EventSuccessfulPrinting item = Event;

            var dbTargetPrinter = context.TargetPrinters
                .FirstOrDefault(e =>e.NameNormalized == (item.TargetPrinter.NameNormalized ?? ""));

            if (dbTargetPrinter != null)
            {
                item.TargetPrinter = dbTargetPrinter;
            }
            else
            {
                var networkPrinter = context.NetworkPrinters
                    .FirstOrDefault(p => p.ShareName == item.TargetPrinter.NameNormalized);

                if (networkPrinter is null)
                {
                    UniquePrinterFounded?.Invoke(item.TargetPrinter.NameNormalized);
                }
                if (networkPrinter != null)
                {
                    item.TargetPrinter.NetworkPrinter = networkPrinter;
                }
                await context.TargetPrinters.AddAsync(item.TargetPrinter);
                await context.SaveChangesAsync();

            }

            var dbSender = context.Senders
                .FirstOrDefault(s => s.NameNormalized == item.Sender.NameNormalized);

            if (dbSender != null)
            {
                item.Sender = dbSender;
            }
            else
            {
                await context.Senders.AddAsync(item.Sender);
                await context.SaveChangesAsync();

            }

            var dbSenderDevice = context.SenderDevices.FirstOrDefault(s => s.NameNormalized == item.SenderDevice.NameNormalized);


            if (dbSenderDevice != null)
            {
                item.SenderDevice = dbSenderDevice;
            }
            else
            {
                await context.SenderDevices.AddAsync(item.SenderDevice);
                await context.SaveChangesAsync();

            }

            var dbSentPrintingFile =

                context.SentPrintingFiles
                .FirstOrDefault(s => s.Name.name == item.SentPrintingFile.Name.name &&
                                          s.Size == item.SentPrintingFile.Size &&
                                          s.Pages == item.SentPrintingFile.Pages);
            if (dbSentPrintingFile != null)
            {
                item.SentPrintingFile = dbSentPrintingFile;
            }
            else
            {
                var documentName = context.DocumentNames.FirstOrDefault(p => p.name == item.SentPrintingFile.Name.name);

                if (documentName != null)
                {

                    item.SentPrintingFile.Name = documentName;
                }
                await context.SentPrintingFiles.AddAsync(item.SentPrintingFile);
                await context.SaveChangesAsync();

            }

            await context.EventsSuccessfulPrinting.AddAsync(item);
            //currentPercent += percentIncrement;

        }
      
        public async Task DbActualizationAsync()
        {
            var eventsListFromJournal = await _eventLogService.Get307PrintEventsListAsync();
            if (eventsListFromJournal is not null)
            {
                var ConvertEvenstListForDB = await ConvertEventPrint307ToEventSuccessfulPrintingAsync(eventsListFromJournal);
                if (ConvertEvenstListForDB is not null)
                {
                    await AddEventsToDBAsync(ConvertEvenstListForDB);

                }
            }


        }

        public async Task<List<EventSuccessfulPrinting>> ConvertEventPrint307ToEventSuccessfulPrintingAsync(List<EventPrint307> EventsList307)
        {


            List<EventSuccessfulPrinting> eventSuccessfulPrinting = new List<EventSuccessfulPrinting>();

            foreach (var eventLog in EventsList307.ToList())
            {
                SentPrintingFile SentFile = new SentPrintingFile(
                   name: eventLog.DocName,
                   size: eventLog.Size,
                   pages: eventLog.Page
                    );
                Sender Sender = new Sender
                {
                    Name = eventLog.UserName,
                };

                SenderDevice SenderDevice = new SenderDevice
                {
                    Name = eventLog.PCName,
                };
                TargetPrinter TargetPrinter = new TargetPrinter
                {
                    Name = eventLog.PrinterName,
                };



                eventSuccessfulPrinting.Add(new EventSuccessfulPrinting
                (
                    dateTime: eventLog.DateTime,
                    sender: Sender,
                    senderDevice: SenderDevice,
                    sentPrintingFile: SentFile,
                    targetPrinter: TargetPrinter

                ));
            }
            return eventSuccessfulPrinting;


        }

    }

}
