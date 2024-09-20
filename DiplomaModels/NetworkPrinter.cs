using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Text.Json.Serialization;

namespace DiplomaModels
{
    public class NetworkPrinter
    {
        
        private string name;

        private string shareName;
       
        private string comment;

        private string ip;

        private string modelName;
        

        [NotMapped]
        private bool? isOnline;

        //private string shareName;



        [JsonIgnore]
        public int Id { get; set; }
        [JsonIgnore]
        public int? PrinterModelId { get; set; } // Внешний ключ
        ///// <summary>
        ///// Объект модели принтера
        ///// </summary>
        [JsonPropertyName("PrinterModel")]
        public virtual PrinterModel? PrinterModel { get; set; } // Навигационное свойство

        ///// <summary>
        ///// имя принтера
        ///// </summary>
        [JsonPropertyName("Name")]
        public string? Name { get; set; }

        ///// <summary>
        ///// имя принтера, которое пользователи видят по сети, когда он находится в общем доступе.
        ///// </summary>
        [JsonPropertyName("ShareName")]
        public string? ShareName { get; set; }



        /// <summary>
        /// Получает или задает комментарий к принтеру.
        /// </summary>
        [JsonPropertyName("comment")]
        public string? Comment { get; set; }

        /// <summary>
        /// Ip адрес принтера
        /// </summary>
        [JsonPropertyName("ip")]
        public string? Ip
        {
            get { return ip; }
            set
            {
               if (value == null) { ip = value; }
               else if (CheckingValidIPAddress(value))
                {
                    ip = value;
                }
                else
                {
                    throw new FormatException("Неверный формат IP-адреса.");
                }
            }
        }
        public string? NonIPAddress {  get; set; }

       
        [JsonPropertyName("isOnline")]
        public bool IsOnline
        {
            get { return isOnline ?? false; }
            set { isOnline = value; }
        }

        private bool CheckingValidIPAddress(string ipAddress)
        {
            IPAddress address;
            return IPAddress.TryParse(ipAddress, out address);
        }

 
    }
}
