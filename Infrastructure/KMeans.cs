using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using A_Diploma.Domain.Interfaces.ClasterMethods;

using Diploma.Application.Interfaces.ClasterMethods;
using Diploma.Domain.Interfaces;
using Diploma.Domain.Interfaces.ClasterMethods;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Diploma.Application.Services
{
    public class KMeans<T> : IClusterAnalyzer<T>, IKMeans
    {

        double[,] _data;
        int _k;
        CentroidInitializationFunc _centroidsInitializer;
        double[,] _centroids;
        int _maxIterations;
        double _threshold;

        public delegate double[,] CentroidInitializationFunc(double[,] _data, int _k);

        public KMeans(double[,] data, int k, CentroidInitializationFunc centroidsInitializer, int maxIterations = 100, double threshold = 1e-6)
        {
            _data = data;
            _k = k;
            _centroidsInitializer = centroidsInitializer;
            _maxIterations = maxIterations;
            _threshold = threshold;
        }   
        public KMeans(double[,] data, int k, double[,] centroids, int maxIterations = 100, double threshold = 1e-6)
        {
            _data = data;
            _k = k;
             _centroids = centroids;
            _maxIterations = maxIterations;
            _threshold = threshold;
        } 
        public KMeans(IKMeansOptions options)
        {
            _data = options.Matrix;
            _k =options.k;
            _centroids = options.Centroids;
            _maxIterations = options.maxIterations;
            _threshold = options.threshold;
        }   
        public KMeans()
        {

        }
        public double[,] Analysis(double[,] data, int k, CentroidInitializationFunc СentroidsInitializer, int maxIterations = 100, double threshold = 1e-6)
        {
            var centroids = СentroidsInitializer(_data, _k);
            return Analysis(data, k, centroids, maxIterations, threshold);
        }

        /// <summary>
        /// Выполняет кластеризацию методом _k-средних для нормализованной матрицы данных.
        /// </summary>
        /// <param name="_data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="_k">Желаемое число кластеров.</param>
        /// <param name="_maxIterations">Максимальное число итераций.</param>
        /// <param name="_threshold">Порог для остановки (изменение центроидов).</param>
        /// <returns>Объект IClusterResultRepository с назначениями кластеров.</returns>
        public double[,] Analysis(double[,] data, int k, double[,] centroids , int maxIterations = 100, double threshold = 1e-6)
        {

            // Количество объектов (строк) в переданном массиве данных
            int numObjects = _data.GetLength(0);
            // Количество признаков (столбцов) у каждого объекта
            int numFeatures = _data.GetLength(1);
            int[] assignments = new int[numObjects];
            bool changed = true;
            int iterations = 0;
            while (changed && iterations < _maxIterations)
            {
                changed = false;
                // Шаг 1: Назначение каждого объекта к ближайшему центроиду.
                for (int i = 0; i < numObjects; i++)
                {
                    double minDist = double.MaxValue;
                    int bestCluster = 0;
                    for (int cluster = 0; cluster < _k; cluster++)
                    {
                        double dist = 0;
                        for (int j = 0; j < numFeatures; j++)
                        {
                            double diff = _data[i, j] - centroids[cluster, j];
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
                double[,] newCentroids = new double[_k, numFeatures];
                int[] counts = new int[_k];
                for (int i = 0; i < numObjects; i++)
                {
                    int cluster = assignments[i];
                    counts[cluster]++;
                    for (int j = 0; j < numFeatures; j++)
                    {
                        newCentroids[cluster, j] += _data[i, j];
                    }
                }
                for (int cluster = 0; cluster < _k; cluster++)
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
                for (int cluster = 0; cluster < _k; cluster++)
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
                if (maxChange < _threshold)
                    break;
            }

            return new { Assignments = assignments };
        }

      

    }
}
