using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces.ClasterMethods;
using Diploma.Domain.Interfaces;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Diploma.Application.Services
{
    public class KMeans : IClusterAnalyzer, IKMeans
    {

        public delegate double[,] CentroidInitializationFunc(double[,] data, int k);

        public KMeans()
        {
        
         
        }

        /// <summary>
        /// Выполняет кластеризацию методом k-средних для нормализованной матрицы данных.
        /// </summary>
        /// <param name="data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="k">Желаемое число кластеров.</param>
        /// <param name="maxIterations">Максимальное число итераций.</param>
        /// <param name="threshold">Порог для остановки (изменение центроидов).</param>
        /// <returns>Объект IClusterResultRepository с назначениями кластеров.</returns>
        public double[,] Analysis(double[,] data, int k, CentroidInitializationFunc СentroidsInitializer, int maxIterations = 100, double threshold = 1e-6)
        {
            

            // Количество объектов (строк) в переданном массиве данных
            int numObjects = data.GetLength(0);
            // Количество признаков (столбцов) у каждого объекта
            int numFeatures = data.GetLength(1);
            int[] assignments = new int[numObjects];
            var centroids = СentroidsInitializer(data, k);
            bool changed = true;
            int iterations = 0;
            while (changed && iterations < maxIterations)
            {
                changed = false;
                // Шаг 1: Назначение каждого объекта к ближайшему центроиду.
                for (int i = 0; i < numObjects; i++)
                {
                    double minDist = double.MaxValue;
                    int bestCluster = 0;
                    for (int cluster = 0; cluster < k; cluster++)
                    {
                        double dist = 0;
                        for (int j = 0; j < numFeatures; j++)
                        {
                            double diff = data[i, j] - centroids[cluster, j];
                            dist += diff * diff;
                        }
                        // Используем квадрат расстояния (без извлечения квадратного корня)
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestCluster = cluster;
                        }
                    }
                    if (assignments[i] != bestCluster)
                    {
                        assignments[i] = bestCluster;
                        changed = true;
                    }
                }

                // Шаг 2: Пересчёт центроидов.
                double[,] newCentroids = new double[k, numFeatures];
                int[] counts = new int[k];
                for (int i = 0; i < numObjects; i++)
                {
                    int cluster = assignments[i];
                    counts[cluster]++;
                    for (int j = 0; j < numFeatures; j++)
                    {
                        newCentroids[cluster, j] += data[i, j];
                    }
                }
                for (int cluster = 0; cluster < k; cluster++)
                {
                    if (counts[cluster] > 0)
                    {
                        for (int j = 0; j < numFeatures; j++)
                        {
                            newCentroids[cluster, j] /= counts[cluster];
                        }
                    }
                    else
                    {
                        // Если кластер пустой, оставляем старый центроид.
                        for (int j = 0; j < numFeatures; j++)
                        {
                            newCentroids[cluster, j] = centroids[cluster, j];
                        }
                    }
                }

                // Проверка критерия останова: вычисляем максимальное изменение центроидов.
                double maxChange = 0;
                for (int cluster = 0; cluster < k; cluster++)
                {
                    double change = 0;
                    for (int j = 0; j < numFeatures; j++)
                    {
                        double diff = newCentroids[cluster, j] - centroids[cluster, j];
                        change += diff * diff;
                    }
                    change = Math.Sqrt(change);
                    if (change > maxChange)
                        maxChange = change;
                }
                centroids = newCentroids;
                iterations++;
                if (maxChange < threshold)
                    break;
            }

            //return new { Assignments = assignments };
        }

        /// <summary>
        /// Метод инициализирует центроиды для алгоритма кластеризации К-средних, позволяя задать центройды в ручную, или выбирая k различных случайных объектов из данных.
        /// </summary>
        /// <param name="data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="k">Желаемое число кластеров.</param>
        /// <returns>Двумерный массив (double[,]) центройдов.</returns>
      static double[,] CentroidInitializer(double[,] data, int k) {
            // Количество объектов (строк) в переданном массиве данных
            int numObjects = data.GetLength(0);
            // Количество признаков (столбцов) у каждого объекта
            int numFeatures = data.GetLength(1);
            double[,] centroids = new double[k, numFeatures];
            Random rand = new Random();
            if (k == 3)
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
                //Инициализация для k, отличного от 3(например, случайным образом)
                var chosenIndices = new HashSet<int>();
                for (int i = 0; i < k; i++)
                {
                    int index;
                    do
                    {
                        index = rand.Next(numObjects);
                    } while (chosenIndices.Contains(index));
                    chosenIndices.Add(index);
                    for (int j = 0; j < numFeatures; j++)
                    {
                        centroids[i, j] = data[index, j];
                    }
                }
            }
            return centroids;
        }

    }
}
