using Diploma.Domain.Interfaces;

namespace Diploma.Application.UseCase
{
    public class ClusterAnalysisUseCase
    {
        private readonly IClusterAnalyzer _analyzer;
        private readonly IObjectToMatrix converter;
        private readonly IClusterAnalyzer _analyzer;

        // Зависимость внедряется через конструктор
        public ClusterAnalysisUseCase(IClusterAnalyzer analyzer)
        {
            _analyzer = analyzer;
        }

        public void Execute()
        {
            //double[,] data = GetData(); // Например, из репозитория
            //int[] clusters = _analyzer.Analyze(data);
            //// Обработка результата...
        }

    }
}
