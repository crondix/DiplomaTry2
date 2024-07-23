using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DiplomaModels
{
    [Index(nameof(name), IsUnique = true)]
    public class DocumentName
    {
       public int id {  get; set; }
       public string name {  get; set; }
    }
}
