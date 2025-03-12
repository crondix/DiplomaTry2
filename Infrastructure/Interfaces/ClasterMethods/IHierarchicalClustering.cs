using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces.Repository;

namespace Diploma.Application.Interfaces.ClasterMethods
{
    public interface IHierarchicalClustering
    {
        public IClusterResultRepository Analysis(double[,] distanceMatrix, int numberOfClusters);
    }
}
