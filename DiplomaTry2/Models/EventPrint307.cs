using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace DiplomaTry2.Models
{
    public class EventPrint307
    {
        private string docName;
        private string userName;
        private string pCName;
        private string printerName;
        private string port;
        private long size;
        private short page;
        private DateTime? dateTime;



        /// <param name="docName">Param1 in Event 307 Properties</param>
        /// <param name="userName">Param2 in Event 307 Properties</param>
        /// <param name="pcName">Param3 in Event 307 Properties</param>
        /// <param name="printerName">Param4 in Event 307 Properties</param>
        /// <param name="size">Param5 in Event 307 Properties</param>
        /// <param name="page">Param6 in Event 307 Properties</param>
        /// <param name="port">Param6 in Event 307 Properties</param>
        /// <param name="dateTime">Дата время события из журнала</param>
        public EventPrint307(string docName, string userName, string pcName, string printerName, long size, short page, string port, DateTime? dateTime)
        {
            DocName = docName;
            UserName = userName;
            PCName = pcName;
            PrinterName = printerName;
            Size = size;
            Page = page;
            Port = port;
            DateTime = dateTime;
        }

        /// <summary>
        /// Param2 in Event 307 Properties
        /// </summary>
        public string DocName { get => docName; set => docName = value; }

        /// <summary>
        /// Param3 in Event 307 Properties
        /// </summary>
        public string UserName { get => userName; set => userName = value; }

        /// <summary>
        /// Param4 in Event 307 Properties
        /// </summary>
        public string PCName { get => pCName; set => pCName = value; }

        /// <summary>
        /// Param5 in Event 307 Properties
        /// </summary>
        public string PrinterName { get => printerName; set => printerName = value; }

        /// <summary>
        /// Param6 in Event 307 Properties
        /// </summary>
        public string Port { get => port; set => port = value; }

        /// <summary>
        /// Param7 in Event 307 Properties
        /// </summary>
        public long Size { get => size; set => size = value; }

        /// <summary>
        /// Param8 in Event 307 Properties
        /// </summary>
        public short Page { get => page; set => page = value; }
        /// <summary>
        /// Дата время события из журнала
        /// </summary>
        public DateTime? DateTime { get => dateTime; set => dateTime = value; }

    }
}
