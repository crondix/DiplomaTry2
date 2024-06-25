using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace DiplomaModels
{
    public class PrinterModel
    {
        public PrinterModel()
        {
             
        }
        [JsonIgnore]
        public int id { get; set; }
        /// <summary>
        /// Название модели принтера
        /// </summary>
        [JsonPropertyName("modelName")]
        public string? ModelName { get; set; }
    }
}
