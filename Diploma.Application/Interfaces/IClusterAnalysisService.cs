using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.UseCase;

namespace Diploma.Application.Interfaces
{
    interface IClusterAnalysisService
    {
        public IMatrixNormalizer Normalizer { get; }
        public IObjectToMatrixConverter objectToMatrix { get; }
        public IClusterResult Analysis();

    }
}
