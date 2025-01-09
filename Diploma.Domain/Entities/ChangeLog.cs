using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiplomaModels
{
    public class ChangeLog
    {
        [Key]
        public string TableName { get; set; }
        public DateTime LastModified { get; set; }

    }
}
