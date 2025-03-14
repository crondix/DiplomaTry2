using System.Linq.Expressions;
using System.Threading.Tasks;

using Diploma.Domain.Entities;
using Diploma.Domain.Interfaces;
using Diploma.Infrastructure.Interfaces.Helpers;

namespace Diploma.Application.UseCase
{
    public class ClusterAnalysisUseCase
    {
        private readonly IClusterAnalyzer _analyzer;
        private readonly IClusterItemRepository _repository;
        private readonly IToMatrixConverter _converter;
        private readonly IMatrixNormalizer _normolaizer;

        // Зависимость внедряется через конструктор
        public ClusterAnalysisUseCase(IClusterAnalyzer analyzer, IToMatrixConverter converter, IMatrixNormalizer normolaizer, IClusterItemRepository repository)
        {
            _analyzer = analyzer;
            _converter = converter;
            _normolaizer = normolaizer;
            _repository = repository;
        }

        public async Task<IEnumerable<ClusterItem>> Execute()
        {
            IEnumerable<ClusterItem> data = await _repository.GetAllAsync();
            Expression<Func<ClusterItem, double>>[] propertySelectors = new[]
        {
            item => item., // Предполагаемые свойства

        };
            var matrix = _converter.Convert(data,);
            var normalaizMatrix = _normolaizer.Normalize();
            IEnumerable<ClusterItem> сlusterizationResult = _analyzer.Analyze();

            return сlusterizationResult;

        }

    }
}
