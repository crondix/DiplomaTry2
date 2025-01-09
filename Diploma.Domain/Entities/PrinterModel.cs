using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using System.Xml.Linq;

using Microsoft.EntityFrameworkCore;

namespace DiplomaModels
{
    [Index(nameof(NormalizedModelName), IsUnique = true)]
    public class PrinterModel
    {
        private string _modelName;
      
        private string _modelNameNormalized;

       

        //[JsonIgnore]
        [Key]
        public int Id { get; set; }
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
        
        public string NormalizedModelName
        {
            get { return _modelNameNormalized; }
            set { _modelNameNormalized = value; }
        }

       public short? MaxCopyPerPrint {  get; set; }
       public bool? IsDuplexing {  get; set; }
       public bool? IsColor {  get; set; }
        public virtual ICollection<PaperSize>? PaperSizes { get; set; }

    }

}
