using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Application.Interfaces
{
    public interface IKMeansService:IMethodClusterAnalysisService
    {
        public delegate double[,] CentroidInitializationFunc(double[,] data, int k);
        public IClusterResultRepository Analysis(double[,] data, int k, CentroidInitializationFunc? СentroidsInitializer, int maxIterations, double threshold);
    }
}
