using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiplomaModels.Interface;

namespace DiplomaModels
{
    public class SenderDevice: ISenderDevice
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
