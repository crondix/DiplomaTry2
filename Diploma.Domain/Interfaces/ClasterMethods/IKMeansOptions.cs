using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A_Diploma.Domain.Interfaces.ClasterMethods
{
    public interface IKMeansOptions:IClusterOptions
    {

        public int k { get; set; }
        public double[,] Centroids { get; set; }
        public int maxIterations { get; set; }
        public double threshold { get; set; }
    }
}
