using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;

namespace Diploma.Application.Services
{
    public class ClusterAnalysisService : IClusterAnalysisService
    {
        IMatrixNormalizer IClusterAnalysisService.Normalizer => throw new NotImplementedException();

        IObjectToMatrixConverter IClusterAnalysisService.objectToMatrix => throw new NotImplementedException();

        IClusterResult IClusterAnalysisService.Analysis()
        {
            throw new NotImplementedException();
        }
    }
}
