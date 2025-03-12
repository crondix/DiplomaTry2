using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Domain.Interfaces
{
    public interface IClusterAnalyzer
    {
        int[] Analyze(double[,] data);
    }
}
