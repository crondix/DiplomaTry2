using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using A_Diploma.Domain.Interfaces.ClasterMethods;

namespace C_Diploma.Infrastructure
{
    internal class KMeansOptions: IKMeansOptions
    {
        public double[,] Matrix { get; set; }
        public int k { get; set; }
        public double[,] Сentroids { get; set; }
        public int maxIterations { get; set; } = 100;
        public double threshold { get; set; } = 1e-6;
    }
}
}
