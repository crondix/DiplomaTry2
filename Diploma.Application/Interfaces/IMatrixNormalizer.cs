using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diploma.Application.Interfaces
{
    public interface IMatrixNormalizer
    {
       public double[,] Normalize(double[,] matrix);
    }
}
