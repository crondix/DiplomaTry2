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
        private double[,]? _matrix { get; set; }

        public MatrixNormalizer(double[,] matrix)  
        {
            _matrix = matrix;
            
        }   
        public MatrixNormalizer()  
        {
        }
        /// <summary>
        /// Нормализует матрицу по столбцам: для каждого столбца выполняется нормализация:
        /// (value - min) / (max - min)
        /// </summary>
        ///<remarks> 
        ///Для данной перегрузки метода используется матрица, которая была передана в конструктор.
        ///</remarks> 
        /// <exception cref="ArgumentNullException">Если в конструктор передана null матрица.</exception>

        public double[,] Normalize()
        {
            if (_matrix != null)
            {
                return Normalize(_matrix);
            }
            else
            {
                throw new ArgumentNullException("Matrix is null");
            }
        }
        /// <summary>
        /// Нормализует матрицу по столбцам: для каждого столбца выполняется нормализация:
        /// (value - min) / (max - min)
        /// </summary>

        public double[,] Normalize(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);
            double[,] normalized = new double[rows, columns];

            for (int j = 0; j < columns; j++)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int i = 0; i < rows; i++)
                {
                    if (matrix[i, j] < min) min = matrix[i, j];
                    if (matrix[i, j] > max) max = matrix[i, j];
                }
                for (int i = 0; i < rows; i++)
                {
                    normalized[i, j] = (matrix[i, j] - min) / (max - min);
                }
            }


            return normalized;
        }
    }
}
