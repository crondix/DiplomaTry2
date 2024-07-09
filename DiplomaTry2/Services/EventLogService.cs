using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

using DiplomaTry2.Models;

namespace DiplomaTry2.Services
{
    public class EventLogService
    {
       

   
        public List<EventRecord>? GetListEventLog(string remoteComputerName, string query, string logName, bool reverseDirection=false)
        {
            // Создание экземпляра EventLogSession для удаленного компьютера
            EventLogSession remoteSession = new EventLogSession(remoteComputerName);

            // Создание экземпляра EventLogQuery для указанного журнала и запроса
            EventLogQuery eventQuery = new EventLogQuery(logName, PathType.LogName, query)
            {
                Session = remoteSession,
                ReverseDirection = reverseDirection
            };
            List<EventRecord> EventList = new List<EventRecord>();
            try
            {
                // Выполние запроса
                using (EventLogReader logReader = new EventLogReader(eventQuery))
                {
                    EventRecord eventInstance;
                    while ((eventInstance = logReader.ReadEvent()) != null)
                    {
                        EventList.Add(eventInstance);
                       
                    }
                    return EventList;

                   

                }
            }
            catch (EventLogException e)
            {
                Console.WriteLine("Произошла ошибка при чтении журнала событий: {0} \n {1}", e.Message, e.InnerException);
                return null;
            }
        }
        public List<EventPrint307>? Get307PrintEvents()
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
                var Events = GetListEventLog(remoteComputerName,
                query,
                logName,
                true
             );

                if (Events != null)
                {
                    foreach (var item in Events)
                    {
                        Events307List.Add(
                            new EventPrint307
                            (
                                docName:
                                item.Properties.ElementAt(2).Value.ToString(),
                                userName:
                                item.Properties.ElementAt(3).Value.ToString(),
                                pcName:
                                item.Properties.ElementAt(4).Value.ToString(),
                                printerName:
                                item.Properties.ElementAt(5).Value.ToString(),
                                port:
                                item.Properties.ElementAt(6).Value.ToString(),
                                size:
                                Convert.ToInt32(item.Properties.ElementAt(7).Value),
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
            catch (EventLogException e)
            {
                Console.WriteLine("Произошла ошибка при создании списка событий 307: {0} \n {1}", e.Message, e.InnerException);

                return null;
            }

        }

    }
}
