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

        public EventLogProcessingService(EventLogService eventLogService, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _eventLogService = eventLogService;
            _contextFactory = contextFactory;
        }


        public async Task AddToDB(List<EventSuccessfulPrinting> events)
        {
            await using (var context = _contextFactory.CreateDbContext())
            {
                try
                {
                    var eventFromDb = await context.EventsSuccessfulPrinting
                            .Include(e => e.Sender)
                            .Include(e => e.SenderDevice)
                            .Include(e => e.SentPrintingFile)
                            .ToListAsync();

                    foreach (var item in events)
                    {
                        var existingEvent = eventFromDb.FirstOrDefault(e =>
                            e.SenderDevice?.NameNormalized == item.SenderDevice?.NameNormalized &&
                            e.TargetPrinter?.NameNormalized == item.TargetPrinter?.NameNormalized &&
                            e.SentPrintingFile?.Name == item.SentPrintingFile?.Name &&
                            e.Sender?.NameNormalized == item.Sender?.NameNormalized &&
                            e.DateTime == item.DateTime);

                        if (existingEvent is null)
                        {
                            var dbTargetPrinter = await context.TargetPrinters
                                .FirstOrDefaultAsync(tp => tp.NameNormalized == (item.TargetPrinter.NameNormalized??""));
                            if (dbTargetPrinter != null)
                            {
                                item.TargetPrinter = dbTargetPrinter;
                            }
                            else
                            {
                                var networkPrinter = await context.NetworkPrinters
                                    .Include(np => np.PrinterModel)
                                    .FirstOrDefaultAsync(p => p.PrinterModel.NormalizedModelName == item.TargetPrinter.NameNormalized);

                                if (networkPrinter != null)
                                {
                                    item.TargetPrinter.NetworkPrinterId = networkPrinter.Id;
                                }
                                await context.TargetPrinters.AddAsync(item.TargetPrinter);
                                await context.SaveChangesAsync();
                                //item.TargetPrinter = item.TargetPrinter;
                            }

                            var dbSender = await context.Senders
                                .FirstOrDefaultAsync(s => s.NameNormalized == item.Sender.NameNormalized);
                            if (dbSender != null)
                            {
                                item.Sender = dbSender;
                            }
                            else
                            {
                                await context.Senders.AddAsync(item.Sender);
                                await context.SaveChangesAsync();
                                //item.Sender = item.Sender;
                            }

                            var dbSenderDevice = await context.SenderDevices
                                .FirstOrDefaultAsync(s => s.NameNormalized == item.SenderDevice.NameNormalized);
                            if (dbSenderDevice != null)
                            {
                                item.SenderDevice = dbSenderDevice;
                            }
                            else
                            {
                                await context.SenderDevices.AddAsync(item.SenderDevice);
                                await context.SaveChangesAsync();
                                //item.SenderDevice = item.SenderDevice;
                            }

                            var dbSentPrintingFile = await context.SentPrintingFiles
                                .FirstOrDefaultAsync(s => s.Name == item.SentPrintingFile.Name &&
                                                          s.Size == item.SentPrintingFile.Size &&
                                                          s.Pages == item.SentPrintingFile.Pages);
                            if (dbSentPrintingFile != null)
                            {
                                item.SentPrintingFile = dbSentPrintingFile;
                            }
                            else
                            {
                                await context.SentPrintingFiles.AddAsync(item.SentPrintingFile);
                                await context.SaveChangesAsync();
                                //item.SentPrintingFile = item.SentPrintingFile;
                            }

                            await context.EventsSuccessfulPrinting.AddAsync(item);
                        }
                    }
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при попытке актуализации данных : \n{ex.Message} \n{ex.InnerException}");
                }
            }
        }

        public async Task DbActualization()
        {
            var eventsListFromJournal = await _eventLogService.Get307PrintEventsListAsync();
            if (eventsListFromJournal is not null)
            {
                var ConvertEvenstListForDB = await ConvertEventPrint307ToEventSuccessfulPrinting(eventsListFromJournal);
                if (ConvertEvenstListForDB is not null)
                {
                    await AddToDB(ConvertEvenstListForDB);
                }
            }


        }
        public async Task<List<EventSuccessfulPrinting>?> ConvertEventPrint307ToEventSuccessfulPrinting(List<EventPrint307>? EventsList307)
        {

            if (EventsList307 != null)
            {
                List<EventSuccessfulPrinting> eventSuccessfulPrinting = new List<EventSuccessfulPrinting>();
                await using (var context = _contextFactory.CreateDbContext())
                {
                    foreach (var eventLog in EventsList307.ToList())
                    {
                        SentPrintingFile SentFile = new SentPrintingFile
                        {
                            Name = eventLog.DocName,
                            Pages = eventLog.Page,
                            Size = eventLog.Size
                        };
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
                            Name = eventLog.PCName,
                        };



                        eventSuccessfulPrinting.Add(  new EventSuccessfulPrinting
                        (
                            dateTime:eventLog.DateTime,
                            sender:Sender,
                            senderDevice:SenderDevice,
                            sentPrintingFile:SentFile,
                            targetPrinter: TargetPrinter

                        ));
                    }
                    return eventSuccessfulPrinting;
                }
            }
            return null;
        }

    }

}
