using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;

namespace DiplomaModels
{
    public class PrinterModel
    {
        private string _modelName;
      
        private string _modelNameNormalized;

        public PrinterModel()
        {
        }
       
       

        /// <summary>
        /// Название модели принтера
        /// </summary>
        [JsonPropertyName("modelName")]
        public string ModelName
        {
            get { return _modelName; }
            set
            {
                _modelName = value;
                _modelNameNormalized = value.ToUpper().Replace(" ", "");
            }
        }
        [Key]
        public string NormalizedModelName
        {
            get { return _modelNameNormalized; }
            set { _modelNameNormalized = value; }
        }
    }

}
