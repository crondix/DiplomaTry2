using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Infrastructure.Interfaces.Helpers
{
    public interface IMatrixNormalizer
    {
       public double[,] Normalize(INumber[,] matrix);
    }
}
