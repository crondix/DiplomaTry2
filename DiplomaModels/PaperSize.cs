using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomaModels
{
    /// <summary>
    /// Класс описывает форматы бумаги A4, A3 и т.д. 
    /// </summary>
    [Index(nameof(Name), IsUnique = true)]
    public class PaperSize
    {


        public int Id { get; set; }
        [Required]
        [MaxLength(40)]
        public string Name { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }

        public override string ToString() => Name;
    }
}
