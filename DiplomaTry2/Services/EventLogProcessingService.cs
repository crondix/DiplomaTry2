using DiplomaModels;

using DiplomaTry2.Data;
using DiplomaTry2.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace DiplomaTry2.Services
{
    public class EventLogProcessingService
    {
        private readonly EventLogService _eventLogService;
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public EventLogProcessingService(EventLogService eventLogService, IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _eventLogService = eventLogService;
            _contextFactory = contextFactory;
        }


        async Task AddToDB(List<EventSuccessfulPrinting> events)
        {
            await using (var context = _contextFactory.CreateDbContext())
            {
                var eventFromDb = context.EventSuccessfulPrintings
                               .Include(e=>e.Sender)
                               .Include(e => e.SenderDevice)
                               .Include(e => e.SentPrintingFile)
                               .ToList();
                foreach (var item in events)
                {
                    if (eventFromDb.FirstOrDefault(e =>
                    e.SenderDevice?.NameNormalized == item.SenderDevice?.NameNormalized
                    &&
                    e.SentPrintingFile?.Name == item.SentPrintingFile?.Name
                    &&
                    e.Sender?.NameNormalized == item.Sender?.NameNormalized
                    &&
                    e.DateTime == item.DateTime

                    ) is null)
                    {
                        context.EventSuccessfulPrintings.Add(item);
                    }
                }
                context.SaveChanges();
               
            }
        }

        async Task<List<EventSuccessfulPrinting>?> ConvertEventPrint307ToEventSuccessfulPrinting(List<EventPrint307>? EventsList307)
        {

                if (EventsList307 != null)
                {
                    List<EventSuccessfulPrinting> eventSuccessfulPrinting = new List<EventSuccessfulPrinting>();
                    await using (var context = _contextFactory.CreateDbContext())
                    {
                        foreach (var eventLog in EventsList307.ToList())
                        {
                            var SentFile = new SentPrintingFile
                            {
                                Name = eventLog.DocName,
                                Pages = eventLog.Page,
                                Size = eventLog.Size
                            };
                            var Sender = new Sender
                            {
                                Name = eventLog.UserName,
                            };

                            var SenderDevice = new SenderDevice
                            {
                                Name = eventLog.PCName,
                            };

                            var NetworkPrinters = context.NetworkPrinters
                                .Include(np => np.PrinterModel)
                                .ToList();
                            var NetworkPrinter = NetworkPrinters.FirstOrDefault(p => p.PrinterModel.NormalizedModelName == SenderDevice.NameNormalized);
                            if (NetworkPrinter is not null)
                            {
                                SenderDevice.NetworkPrinterId = NetworkPrinter.Id;
                            }

                            EventSuccessfulPrinting printEvent = new EventSuccessfulPrinting
                            {
                                SenderDevice = SenderDevice,
                                SentPrintingFile = SentFile,
                                Sender = Sender,
                                DateTime = eventLog.DateTime
                            };
                            eventSuccessfulPrinting.Add(printEvent);
                        }
                        return eventSuccessfulPrinting;
                    }
                }
                return null;
            }
        
    }

}
