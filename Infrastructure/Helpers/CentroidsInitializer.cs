using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_Diploma.Infrastructure.Helpers
{
    public class CentroidsInitializer
    {
        /// <summary>
        /// Метод инициализирует центроиды для алгоритма кластеризации К-средних, позволяя задать центройды в ручную, или выбирая _k различных случайных объектов из данных.
        /// </summary>
        /// <param name="_data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="_k">Желаемое число кластеров.</param>
        /// <returns>Двумерный массив (double[,]) центройдов.</returns>
        public static double[,] CentroidInitializer(double[,] _data, int _k)
        {
            // Количество объектов (строк) в переданном массиве данных
            int numObjects = _data.GetLength(0);
            // Количество признаков (столбцов) у каждого объекта
            int numFeatures = _data.GetLength(1);
            double[,] centroids = new double[_k, numFeatures];
            Random rand = new Random();
            if (_k == 3)
            {
                //Первый центроид: все признаки = 1
                for (int j = 0; j < numFeatures; j++)
                    centroids[0, j] = 0;
                //Второй центроид: все признаки = 0.5
                for (int j = 0; j < numFeatures; j++)
                    centroids[1, j] = 0.5;
                //Третий центроид: все признаки = 0
                for (int j = 0; j < numFeatures; j++)
                    centroids[2, j] = 1;
            }
            else
            {
                //Инициализация для _k, отличного от 3(например, случайным образом)
                var chosenIndices = new HashSet<int>();
                for (int i = 0; i < _k; i++)
                {
                    int index;
                    do
                    {
                        index = rand.Next(numObjects);
                    } while (chosenIndices.Contains(index));
                    chosenIndices.Add(index);
                    for (int j = 0; j < numFeatures; j++)
                    {
                        centroids[i, j] = _data[index, j];
                    }
                }
            }
            return centroids;
        }
    }
}
