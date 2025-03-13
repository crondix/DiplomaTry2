using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Diploma.Infrastructure.Interfaces.Helpers;

namespace Diploma.Application.Helpers
{
    class MatrixNormalizer : IMatrixNormalizer
    {
        private double[,] _matrix;
        public MatrixNormalizer(double[,] matrix)  
        {
            _matrix = matrix;
        }
        /// <summary>
        /// Нормализует матрицу по столбцам: для каждого столбца выполняется нормализация:
        /// (value - min) / (max - min)
        /// </summary>
        public double[,] Normalize()
        {
            int rows = _matrix.GetLength(0);
            int columns = _matrix.GetLength(1);
            double[,] normalized = new double[rows, columns];

            for (int j = 0; j < columns; j++)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int i = 0; i < rows; i++)
                {
                    if (_matrix[i, j] < min) min = _matrix[i, j];
                    if (_matrix[i, j] > max) max = _matrix[i, j];
                }
                for (int i = 0; i < rows; i++)
                {
                    normalized[i, j] = (_matrix[i, j] - min) / (max - min);
                }
            }


            return normalized;
        }
    }
}
