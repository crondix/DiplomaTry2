using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static System.Runtime.InteropServices.JavaScript.JSType;




namespace Diploma.Domain.Interfaces.ClasterMethods
{
    public interface IKMeans<T>
    {
        public delegate double[,] CentroidInitializationFunc(double[,] _data, int _k);
        public int[] Analysis(double[,] data, int k, CentroidInitializationFunc СentroidsInitializer, int maxIterations = 100, double threshold = 1e-6);
        public int[] Analysis(double[,] data, int k, double[,] centroids, int maxIterations = 100, double threshold = 1e-6);
       
    }
}
