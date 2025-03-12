using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Diploma.Infrastructure.Interfaces.Helpers;

namespace Diploma.Application.Helpers
{
    class ToMatrixConverter : IObjectToMatrixConverter
    {
        /// <summary>
        /// Преобразует массив объектов в двумерный массив чисел (матрицу),  
        /// используя указанные свойства объектов.
        /// </summary>
        /// <typeparam name="T">Тип объектов в массиве.</typeparam>
        /// <param name="objects">Массив объектов, которые будут преобразованы в матрицу.</param>
        /// <param name="propertySelectors">Массив выражений, определяющих свойства объектов,  
        /// которые будут извлекаться в виде чисел.</param>
        /// <returns>Двумерный массив (матрица), где строки соответствуют объектам,  
        /// а столбцы — выбранным свойствам.</returns>
        public double[,] ObjectsToMatrix<T>(T[] objects, Expression<Func<T, double>>[] propertySelectors)
        {
            var propertyFuncs = propertySelectors.Select(selector => selector.Compile()).ToArray();
            double[,] matrix = new double[objects.Length, propertyFuncs.Length];
            for (int i = 0; i < objects.Length; i++)
            {
                T obj = objects[i] ?? throw new ArgumentNullException($"Объект с индексом {i} равен null.");
                var values = propertyFuncs.Select(func => func(obj)).ToArray();
                for (int j = 0; j < values.Length; j++)
                {
                    matrix[i, j] = values[j];
                }
            }
            return matrix;
        }
      
    }
}
