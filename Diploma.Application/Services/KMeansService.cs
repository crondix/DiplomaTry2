using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Diploma.Application.Interfaces;

namespace Diploma.Application.Services
{
    public class KMeansService : IClusterAnalysisService
    {
        public IMatrixNormalizer _Normalizer { get; set; }

        public IObjectToMatrixConverter _ObjectToMatrix { get; set; }

        public KMeansService(IMatrixNormalizer Normalizer, IObjectToMatrixConverter ObjectToMatrix)
        {
            _Normalizer = Normalizer;
            _ObjectToMatrix= ObjectToMatrix;
        }
        /// <summary>
        /// Выполняет кластеризацию методом k-средних для нормализованной матрицы данных.
        /// </summary>
        /// <param name="data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="k">Желаемое число кластеров.</param>
        /// <param name="maxIterations">Максимальное число итераций.</param>
        /// <param name="threshold">Порог для остановки (изменение центроидов).</param>
        /// <returns>Объект IClusterResultItem с назначениями кластеров.</returns>
        public IClusterResultRepository Analysis(double[,] data, int k, int maxIterations = 100, double threshold = 1e-6)
        {
            int numObjects = data.GetLength(0);
            int numFeatures = data.GetLength(1);
            int[] assignments = new int[numObjects];

            // Инициализация центроидов: выбираем k различных случайных объектов из данных.
            double[,] centroids = new double[k, numFeatures];
            Random rand = new Random();
            //if (k == 3)
            //{
            // Первый центроид: все признаки = 1
            for (int j = 0; j < numFeatures; j++)
                centroids[0, j] = 0;
            // Второй центроид: все признаки = 0.5
            for (int j = 0; j < numFeatures; j++)
                centroids[1, j] = 0.5;
            // Третий центроид: все признаки = 0
            for (int j = 0; j < numFeatures; j++)
                centroids[2, j] = 1;
            //}
            //else
            //{
            // Инициализация для k, отличного от 3 (например, случайным образом)
            //var chosenIndices = new HashSet<int>();
            //for (int i = 0; i < k; i++)
            //{
            //    int index;
            //    do
            //    {
            //        index = rand.Next(numObjects);
            //    } while (chosenIndices.Contains(index));
            //    chosenIndices.Add(index);
            //    for (int j = 0; j < numFeatures; j++)
            //    {
            //        centroids[i, j] = data[index, j];
            //    }
            //}
            //}

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

            return new  { Assignments = assignments, Centroids = centroids };
        }



    }
}
