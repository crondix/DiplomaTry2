using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

using DiplomaTry2.Models;

namespace DiplomaTry2.Services
{
    public class EventLogService
    {



        public async Task<List<EventRecord>?> GetListEventLogAsync(string remoteComputerName, string query, string logName, bool reverseDirection = false)
        {
            // Создание экземпляра EventLogSession для удаленного компьютера
            EventLogSession remoteSession = new EventLogSession(remoteComputerName);

            // Создание экземпляра EventLogQuery для указанного журнала и запроса
            EventLogQuery eventQuery = new EventLogQuery(logName, PathType.LogName, query)
            {
                Session = remoteSession,
                ReverseDirection = reverseDirection
            };

            List<EventRecord> eventList = new List<EventRecord>();

            try
            {
                // Выполнение запроса в отдельном потоке
                await Task.Run(() =>
                {
                    using (EventLogReader logReader = new EventLogReader(eventQuery))
                    {
                        EventRecord eventInstance;
                        while ((eventInstance = logReader.ReadEvent()) != null)
                        {
                            eventList.Add(eventInstance);
                        }
                    }
                });

                return eventList;
            }
            catch (EventLogException e)
            {
                Console.WriteLine("Произошла ошибка при чтении журнала событий: {0} \n {1}", e.Message, e.InnerException);
                return null;
            }
        }
        public async  Task<List<EventPrint307>?> Get307PrintEventsListAsync()
        {
            //Имя удаленного пк/сервера
            string remoteComputerName = @"vm-print.kombinat.ru";

            // Имя журнала
            string logName = "Microsoft-Windows-PrintService/Operational";

            // Строка для запроса событий
            string query = "*[System/EventID=307]";
            List<EventPrint307> Events307List = new List<EventPrint307>();
            try
            {
                var Events = await GetListEventLogAsync(remoteComputerName,
                query,
                logName,
                true
             );

                if (Events != null)
                {
                    
                    foreach (var item in Events)
                    {
                        Console.WriteLine($"Docname: {item.Properties.ElementAt(1).Value}," +
                            $" userName: {item.Properties.ElementAt(2).Value}, " +
                            $"pcName: {item.Properties.ElementAt(3).Value} "+
                            $"PrinterName: {item.Properties.ElementAt(4).Value} " +
                            $"Size: {item.Properties.ElementAt(6).Value} " +
                            $"Page: {item.Properties.ElementAt(7).Value} " +
                            $"Data: {item.TimeCreated ?? null}"
                            );
                        Events307List.Add(
                            new EventPrint307
                            (
                                docName:
                                item.Properties.ElementAt(1).Value.ToString(),
                                userName:
                                item.Properties.ElementAt(2).Value.ToString(),
                                pcName:
                                item.Properties.ElementAt(3).Value.ToString(),
                                printerName:
                                item.Properties.ElementAt(4).Value.ToString(),
                                port:
                                item.Properties.ElementAt(5).Value.ToString(),
                                size:
                                Convert.ToInt64(item.Properties.ElementAt(6).Value),
                                page:
                                Convert.ToInt16(item.Properties.ElementAt(7).Value),
                                dateTime:
                                item.TimeCreated??null
                            ));
                    }
                    return Events307List;
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Произошла ошибка при создании списка событий 307: {0} \n {1} \n {2} \n {3}", e.Message, e.InnerException, e.Data, e.StackTrace );

                return null;
            }
           

        }

    }
}
