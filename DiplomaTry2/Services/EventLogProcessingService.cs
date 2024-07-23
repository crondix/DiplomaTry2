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
                    //var eventFromDb = await context.EventsSuccessfulPrinting.ToListAsync();

                    List<EventSuccessfulPrinting>? exist;
                    //var exist = events.ExceptBy(context.EventsSuccessfulPrinting, o => o.DateTime);
                    var lastEventDB = context.EventsSuccessfulPrinting.OrderByDescending(o => o.DateTime).FirstOrDefault();

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
                    //foreach ( in exist)
                    //{
                    double proccent = 0.01;
                   
                    for (var i = 0; i<exist.Count; i++) {
                        
                        if ( i == Math.Round((exist.Count * proccent)))
                        {
                            
                            Console.Write($"\rCompleted: {Math.Round(proccent * 100)}%");
                            proccent += 0.01;
                        }
                            EventSuccessfulPrinting item=exist[i];
                        var networkPrinters = await context.NetworkPrinters.ToListAsync();
                        var dbTargetPrinters = await context.TargetPrinters.ToListAsync();
                        var dbSenders = await context.Senders.ToListAsync();
                        var dbSenderDevices = await context.SenderDevices.ToListAsync();
                        var dbSentPrintingFiles = await context.SentPrintingFiles.ToListAsync();
                        var dbDocumentName = await context.DocumentNames.ToListAsync();


                        var dbTargetPrinter = dbTargetPrinters.FirstOrDefault(e =>
                       e.NameNormalized == (item.TargetPrinter?.NameNormalized ?? "")
                        );
                        ////.TargetPrinters
                        ////.FirstOrDefaultAsync(tp => tp.NameNormalized == (item.TargetPrinter.NameNormalized??""));
                        if (dbTargetPrinter != null)
                        {
                            item.TargetPrinter = dbTargetPrinter;
                        }
                        else
                        {
                            var networkPrinter = networkPrinters.FirstOrDefault(p => p.ShareName == item?.TargetPrinter?.NameNormalized);

                            if (networkPrinter != null)
                            {
                                item.TargetPrinter.NetworkPrinterId = networkPrinter.Id;
                            }
                            await context.TargetPrinters.AddAsync(item.TargetPrinter);
                            await context.SaveChangesAsync();
                            dbTargetPrinters.Add(item.TargetPrinter);
                            //await context.TargetPrinters.ToListAsync();
                            //item.TargetPrinter = item.TargetPrinter;
                        }

                        var dbSender = dbSenders.FirstOrDefault(s => s.NameNormalized == item.Sender.NameNormalized);
                        //await context.Senders
                        //.FirstOrDefaultAsync(s => s.NameNormalized == item.Sender.NameNormalized);
                        if (dbSender != null)
                        {
                            item.Sender = dbSender;
                        }
                        else
                        {
                            await context.Senders.AddAsync(item.Sender);
                            await context.SaveChangesAsync();
                            dbSenders.Add(item.Sender);
                            //await context.Senders.ToListAsync();
                            //item.Sender = item.Sender;
                        }

                        var dbSenderDevice = dbSenderDevices.FirstOrDefault(s => s.NameNormalized == item.SenderDevice.NameNormalized);
                        //await context.SenderDevices

                        if (dbSenderDevice != null)
                        {
                            item.SenderDevice = dbSenderDevice;
                        }
                        else
                        {
                            await context.SenderDevices.AddAsync(item.SenderDevice);
                            await context.SaveChangesAsync();
                            dbSenderDevices.Add(item.SenderDevice);
                            //= await context.SenderDevices.ToListAsync();
                            //item.SenderDevice = item.SenderDevice;
                        }

                        var dbSentPrintingFile =
                            //await context.SentPrintingFiles
                            dbSentPrintingFiles
                            .FirstOrDefault(s => s.Name.name == item.SentPrintingFile.Name.name &&
                                                      s.Size == item.SentPrintingFile.Size &&
                                                      s.Pages == item.SentPrintingFile.Pages);
                        if (dbSentPrintingFile != null)
                        {
                            item.SentPrintingFile = dbSentPrintingFile;
                        }
                        else
                        {
                            var documentName = dbDocumentName.FirstOrDefault(p => p.name == item?.SentPrintingFile?.Name.name);

                            if (documentName != null)
                            {
                                //item.SentPrintingFile.Name = new DocumentName();
                                item.SentPrintingFile.Name = documentName;
                            }
                            await context.SentPrintingFiles.AddAsync(item.SentPrintingFile);
                            await context.SaveChangesAsync();
                            dbSentPrintingFiles.Add(item.SentPrintingFile);
                            //= await context.SentPrintingFiles.ToListAsync();
                            //item.SentPrintingFile = item.SentPrintingFile;
                        }

                        await context.EventsSuccessfulPrinting.AddAsync(item);

                    }
                    
                    await context.SaveChangesAsync();
                    Console.WriteLine($"\nAdding Events Completed.");









                    //foreach (EventSuccessfulPrinting item in events)
                    //{
                    //    var existingEvent = context.EventsSuccessfulPrinting.OrderByDescending(o=>o.DateTime).FirstOrDefault(e =>
                    //        e.DateTime < item.DateTime);


                    //    if (existingEvent is not null)
                    //    {
                    //        var networkPrinters = await context.NetworkPrinters.ToListAsync();
                    //        var dbTargetPrinters = await context.TargetPrinters.ToListAsync();
                    //        var dbSenders = await context.Senders.ToListAsync();
                    //        var dbSenderDevices = await context.SenderDevices.ToListAsync();
                    //        var dbSentPrintingFiles = await context.SentPrintingFiles.ToListAsync();
                    //        var dbDocumentName = await context.DocumentNames.ToListAsync();


                    //        var dbTargetPrinter = dbTargetPrinters.FirstOrDefault(e =>
                    //       e.NameNormalized == (item.TargetPrinter?.NameNormalized ?? "")
                    //        );
                    //        ////.TargetPrinters
                    //        ////.FirstOrDefaultAsync(tp => tp.NameNormalized == (item.TargetPrinter.NameNormalized??""));
                    //        if (dbTargetPrinter != null)
                    //        {
                    //            item.TargetPrinter = dbTargetPrinter;
                    //        }
                    //        else
                    //        {
                    //            var networkPrinter = networkPrinters.FirstOrDefault(p => p.ShareName == item?.TargetPrinter?.NameNormalized);

                    //            if (networkPrinter != null)
                    //            {
                    //                item.TargetPrinter.NetworkPrinterId = networkPrinter.Id;
                    //            }
                    //            await context.TargetPrinters.AddAsync(item.TargetPrinter);
                    //            await context.SaveChangesAsync();
                    //            dbTargetPrinters.Add(item.TargetPrinter);
                    //            //await context.TargetPrinters.ToListAsync();
                    //            //item.TargetPrinter = item.TargetPrinter;
                    //        }

                    //        var dbSender = dbSenders.FirstOrDefault(s => s.NameNormalized == item.Sender.NameNormalized);
                    //        //await context.Senders
                    //        //.FirstOrDefaultAsync(s => s.NameNormalized == item.Sender.NameNormalized);
                    //        if (dbSender != null)
                    //        {
                    //            item.Sender = dbSender;
                    //        }
                    //        else
                    //        {
                    //            await context.Senders.AddAsync(item.Sender);
                    //            await context.SaveChangesAsync();
                    //            dbSenders.Add(item.Sender);
                    //            //await context.Senders.ToListAsync();
                    //            //item.Sender = item.Sender;
                    //        }

                    //        var dbSenderDevice = dbSenderDevices.FirstOrDefault(s => s.NameNormalized == item.SenderDevice.NameNormalized);
                    //        //await context.SenderDevices

                    //        if (dbSenderDevice != null)
                    //        {
                    //            item.SenderDevice = dbSenderDevice;
                    //        }
                    //        else
                    //        {
                    //            await context.SenderDevices.AddAsync(item.SenderDevice);
                    //            await context.SaveChangesAsync();
                    //            dbSenderDevices.Add(item.SenderDevice);
                    //            //= await context.SenderDevices.ToListAsync();
                    //            //item.SenderDevice = item.SenderDevice;
                    //        }

                    //        var dbSentPrintingFile =
                    //            //await context.SentPrintingFiles
                    //            dbSentPrintingFiles
                    //            .FirstOrDefault(s => s.Name.name == item.SentPrintingFile.Name.name &&
                    //                                      s.Size == item.SentPrintingFile.Size &&
                    //                                      s.Pages == item.SentPrintingFile.Pages);
                    //        if (dbSentPrintingFile != null)
                    //        {
                    //            item.SentPrintingFile = dbSentPrintingFile;
                    //        }
                    //        else
                    //        {
                    //            var documentName = dbDocumentName.FirstOrDefault(p => p.name == item?.SentPrintingFile?.Name.name);

                    //            if (documentName != null)
                    //            {
                    //                item.SentPrintingFile.Name.id = documentName.id;
                    //            }
                    //            await context.SentPrintingFiles.AddAsync(item.SentPrintingFile);
                    //            await context.SaveChangesAsync();
                    //            dbSentPrintingFiles.Add(item.SentPrintingFile);
                    //            //= await context.SentPrintingFiles.ToListAsync();
                    //            //item.SentPrintingFile = item.SentPrintingFile;
                    //        }

                    //        await context.EventsSuccessfulPrinting.AddAsync(item);
                    //    }
                    //}
                    //await context.SaveChangesAsync();

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
        public async Task<List<EventSuccessfulPrinting>> ConvertEventPrint307ToEventSuccessfulPrinting(List<EventPrint307> EventsList307)
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
