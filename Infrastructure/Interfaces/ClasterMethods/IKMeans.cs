using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces.Repository;
using Diploma.Application.Interfaces.Service;

namespace Diploma.Application.Interfaces.ClasterMethods
{
    public interface IKMeans:IMethodClusterAnalysisService
    {
        public IClusterResultRepository Analysis(double[,] distanceMatrix, int k, int maxIterations, double threshold);
    }
}
