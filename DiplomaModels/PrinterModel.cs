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
                _modelNameNormalized = value.Trim().ToUpperInvariant().Replace(" ", "");
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
        [MaxLength(80)] public string? DeviceType { get; set; }
        [MaxLength(200)] public string? DeviceFunctions { get; set; }
        [MaxLength(80)] public string? PrintTechnology { get; set; }
        [MaxLength(40)] public string? MaximumPaperFormat { get; set; }
        [MaxLength(100)] public string? MonochromeResolution { get; set; }
        public int? RecommendedMonthlyVolume { get; set; }
        public int? MaximumMonthlyVolume { get; set; }
        [MaxLength(200)] public string? Interfaces { get; set; }
        [JsonIgnore] public byte[]? ImageData { get; set; }
        [MaxLength(40)] public string? ImageContentType { get; set; }
        public virtual ICollection<PaperSize>? PaperSizes { get; set; }

    }

}
