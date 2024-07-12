using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace DiplomaModels
{
    [Index(nameof(Code), IsUnique = true)]
    public class PrintserverEvent { 
    
        public int Id {  get; set; }
        public short Code {  get; set; }
        public string Description {  get; set; }
    }
}
