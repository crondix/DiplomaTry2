using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using A_Diploma.Domain.Interfaces;

using Diploma.Domain.Entities;

namespace Diploma.Domain.Interfaces
{
    public interface IClusterAnalyzer<T>
    {
        public IClusterOptions Options { get; set; }
        public ICollection<T> Execute();
        
    }
}
