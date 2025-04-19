using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using A_Diploma.Domain.Interfaces;
using A_Diploma.Domain.Interfaces.ClasterMethods;

using C_Diploma.Infrastructure;
using C_Diploma.Infrastructure.Helpers;

using Diploma.Application.Interfaces.ClasterMethods;
using Diploma.Domain.Interfaces;
using Diploma.Domain.Interfaces.ClasterMethods;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Diploma.Application.Services
{
    public class KMeans<T> : IClusterAnalyzer<T>, IKMeans<T>
    {

        double[,]? _data;
        int? _k;
        CentroidInitializationFunc? _centroidsInitializer ;
        double[,]? _centroids;
        int _maxIterations = 100;
        double _threshold = 1e-6;
        // «Внутреннее» свойство (конкретный тип):
         IKMeansOptions? Опции { get; set; }

        // Явная реализация интерфейса IClusterAnalyzer<T>.Options:
        IClusterOptions IClusterAnalyzer<T>.Options
        {
            get => Опции
                   ?? throw new InvalidOperationException("Свойство IKMeansOptions (Опции) не инициализировано.");
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Значение Options не может быть null.");
                }

                if (value is not IKMeansOptions опции)
                {
                    throw new ArgumentException(
                        $"Ожидается значение типа {nameof(IKMeansOptions)}, получен {value.GetType().Name}.",
                        nameof(value)
                    );
                }

                Опции = опции;
            }
        }



        public delegate double[,] CentroidInitializationFunc(double[,] _data, int _k);

        #region Конструкторы
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
        #endregion

        public ICollection<T>? Execute() {


            ArgumentNullException.ThrowIfNull(_data);
            int[]? t = AnalysisWithCustomCentroids(Опции.Matrix, Опции.k, Опции.Centroids, Опции.maxIterations, Опции.threshold);
                if (t is null) return null;
                return t.Select(x => (T)Convert.ChangeType(x, typeof(T))).ToList(); 
           
        }



        int[] AnalysisWithCustomCentroidInitializationFunc(double[,] data, int k, CentroidInitializationFunc СentroidsInitializer, int maxIterations = 100, double threshold = 1e-6)
        {
            return AnalysisWithCustomCentroids(data, k, СentroidsInitializer(data, k), maxIterations, threshold);
        }
        int[] Analysis(double[,] data, int k, int maxIterations = 100, double threshold = 1e-6)
        {
            return AnalysisWithCustomCentroids(data, k, _InitCentroids(data, k), maxIterations, threshold);
        }

        /// <summary>
        /// Выполняет кластеризацию методом _k-средних для нормализованной матрицы данных.
        /// </summary>
        /// <param name="_data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="_k">Желаемое число кластеров.</param>
        /// <param name="_maxIterations">Максимальное число итераций.</param>
        /// <param name="_threshold">Порог для остановки (изменение центроидов).</param>
        /// <returns>Объект IClusterResultRepository с назначениями кластеров.</returns>
        int[] AnalysisWithCustomCentroids(double[,] data, int k, double[,] startCentroids , int maxIterations = 100, double threshold = 1e-6)
        {

            // Количество объектов (строк) в переданном массиве данных
            int numObjects = _data.GetLength(0);
            // Количество признаков (столбцов) у каждого объекта
            int numFeatures = _data.GetLength(1);
            int[] assignments = new int[numObjects];
            bool changed = true;
            int iterations = 0;
            double[,] centroids = startCentroids;
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
                double[,] newCentroids = new double[k, numFeatures];
                int[] counts = new int[k];
                for (int i = 0; i < numObjects; i++)
                {
                    int cluster = assignments[i];
                    counts[cluster]++;
                    for (int j = 0; j < numFeatures; j++)
                    {
                        newCentroids[cluster, j] += _data[i, j];
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
                if (maxChange < _threshold)
                    break;
            }

            return  assignments ;
        }

        /// <summary>
        /// Метод инициализирует центроиды для алгоритма кластеризации К-средних, позволяя задать центройды в ручную, или выбирая _k различных случайных объектов из данных.
        /// </summary>
        /// <param name="_data">Нормализованная матрица данных (каждая строка – объект, столбцы – признаки).</param>
        /// <param name="_k">Желаемое число кластеров.</param>
        /// <returns>Двумерный массив (double[,]) центройдов.</returns>
        /// <summary>
        /// Выбирает начальные центроиды.
        /// Если k == 3 — три фиксированные точки <1, 0.5, 0>.
        /// Иначе — k случайных наблюдений из набора данных.
        /// </summary>
        private double[,] _InitCentroids(double[,] data, int k)
        {


            if (k <= 0)
                throw new ArgumentOutOfRangeException(nameof(k), k, "k должно быть > 0.");

            int numObjects = data.GetLength(0);
            int numFeatures = data.GetLength(1);

            if (k > numObjects)
                throw new ArgumentOutOfRangeException(nameof(k), k,
                    "k не может превышать количество объектов в наборе данных.");

            var centroids = new double[k, numFeatures];

            // Случайная выборка k уникальных объектов
            var rand = Random.Shared;
            var chosenIndices = new HashSet<int>();

            while (chosenIndices.Count < k)
                chosenIndices.Add(rand.Next(numObjects));

            int row = 0;
            foreach (int idx in chosenIndices)
            {
                for (int j = 0; j < numFeatures; j++)
                    centroids[row, j] = data[idx, j];
                row++;
            }

            return centroids;
        }

    }
}
