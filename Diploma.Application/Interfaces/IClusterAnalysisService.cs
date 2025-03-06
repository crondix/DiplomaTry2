using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.UseCase;

namespace Diploma.Application.Interfaces
{
    public interface IClusterAnalysisService
    {
   
        public IClusterResultRepository Analysis();

    }
}
